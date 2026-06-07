using ExpenseTracker.Auth.Entities;
using ExpenseTracker.Auth.Models;
using ExpenseTracker.Auth.Repositories;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using OtpNet;
using System.Security.Cryptography;

namespace ExpenseTracker.Auth.Services;

public sealed class AuthService(
    IUserRepository users,
    IInviteTokenRepository invites,
    IPasswordHasher hasher,
    ITotpEncryptor totpEncryptor,
    ILogger<AuthService> logger) : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private const int TotpWindowSteps = 1;
    private const int TotpSecretBytes = 20;

    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByUsernameAsync(request.Username, ct)
            ?? throw new UnauthorizedException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is inactive.");

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTimeOffset.UtcNow)
            throw new UnauthorizedException("Account is temporarily locked. Try again later.");

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                logger.LogWarning("Account {Username} locked after {Attempts} failed attempts", request.Username, user.FailedLoginAttempts);
            }
            await users.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid credentials.");
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await users.SaveChangesAsync(ct);

        logger.LogInformation("User {Username} logged in", user.Username);
        return new LoginResponse(user.Id, user.Username, user.Role.ToString(), user.MfaEnabled);
    }

    public async Task<InviteResponse> CreateInviteAsync(InviteRequest request, Guid invitedByUserId, CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            throw new ValidationException($"Invalid role '{request.Role}'. Valid values: Owner, AdultMember, RestrictedMember.");

        var existing = await users.FindByUsernameAsync(request.Username, ct);
        if (existing is not null)
            throw new ConflictException($"Username '{request.Username}' is already taken.");

        var token = new InviteToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            InvitedUsername = request.Username,
            AssignedRole = role,
            InvitedByUserId = invitedByUserId
        };

        await invites.AddAsync(token, ct);
        await invites.SaveChangesAsync(ct);

        logger.LogInformation("Invite created for {Username} with role {Role}", request.Username, role);
        return new InviteResponse(token.Token, token.ExpiresAt);
    }

    public async Task<ActivateResponse> ActivateAccountAsync(ActivateRequest request, CancellationToken ct = default)
    {
        var invite = await invites.FindByTokenAsync(request.Token, ct)
            ?? throw new NotFoundException("InviteToken", request.Token);

        if (invite.IsUsed)
            throw new ConflictException("This invite link has already been used.");

        if (invite.ExpiresAt < DateTimeOffset.UtcNow)
            throw new ValidationException("This invite link has expired. Request a new one.");

        var user = new User
        {
            Username = invite.InvitedUsername,
            PasswordHash = hasher.Hash(request.Password),
            Role = invite.AssignedRole
        };

        invite.IsUsed = true;

        await users.AddAsync(user, ct);
        await invites.SaveChangesAsync(ct);
        await users.SaveChangesAsync(ct);

        logger.LogInformation("Account activated for {Username}", user.Username);
        return new ActivateResponse(user.Id, user.Username, user.Role.ToString());
    }

    public async Task<MfaSetupResponse> SetupMfaAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        // Generate secret and return for display only; do NOT persist until verify succeeds.
        var secretBytes = KeyGeneration.GenerateRandomKey(TotpSecretBytes);
        var base32Secret = Base32Encoding.ToString(secretBytes);

        var otpUri = $"otpauth://totp/ExpenseTracker:{Uri.EscapeDataString(user.Username)}?secret={base32Secret}&issuer=ExpenseTracker";

        logger.LogInformation("MFA setup initiated for user {UserId}", userId);
        return new MfaSetupResponse(base32Secret, otpUri);
    }

    public async Task EnableMfaAsync(Guid userId, MfaEnableRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        // Validate the OTP against the provided secret before persisting anything.
        var secretBytes = Base32Encoding.ToBytes(request.Secret);
        var totp = new Totp(secretBytes);
        var isValid = totp.VerifyTotp(
            request.Code,
            out _,
            new VerificationWindow(previous: TotpWindowSteps, future: TotpWindowSteps));

        if (!isValid)
            throw new ValidationException("Invalid OTP code. Please check your authenticator app and try again.");

        user.TotpSecretEncrypted = totpEncryptor.Encrypt(request.Secret);
        user.MfaEnabled = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await users.SaveChangesAsync(ct);

        logger.LogInformation("MFA enabled for user {UserId}", userId);
    }

    public async Task<string> VerifyMfaLoginAsync(Guid userId, MfaLoginRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (!user.MfaEnabled || user.TotpSecretEncrypted is null)
            throw new ValidationException("MFA is not enabled for this account.");

        var base32Secret = totpEncryptor.Decrypt(user.TotpSecretEncrypted);
        var secretBytes = Base32Encoding.ToBytes(base32Secret);
        var totp = new Totp(secretBytes);
        var isValid = totp.VerifyTotp(
            request.Code,
            out _,
            new VerificationWindow(previous: TotpWindowSteps, future: TotpWindowSteps));

        if (!isValid)
            throw new UnauthorizedException("Invalid MFA code.");

        logger.LogInformation("MFA login verified for user {UserId}", userId);
        return user.Role.ToString();
    }

    public async Task AdminToggleMfaAsync(Guid targetUserId, bool enabled, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(targetUserId, ct)
            ?? throw new NotFoundException("User", targetUserId);

        if (!enabled)
        {
            user.MfaEnabled = false;
            user.TotpSecretEncrypted = null;
            logger.LogInformation("MFA disabled for user {UserId} by admin", targetUserId);
        }
        else
        {
            // Enabling via admin requires the user to then set up MFA themselves.
            // We set the flag to prompt them; the TOTP secret is not set until setup completes.
            user.MfaEnabled = true;
            logger.LogInformation("MFA flag enabled for user {UserId} by admin (setup required)", targetUserId);
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await users.SaveChangesAsync(ct);
    }
}
