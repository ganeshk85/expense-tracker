using ExpenseTracker.Auth.Models;

namespace ExpenseTracker.Auth.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<InviteResponse> CreateInviteAsync(InviteRequest request, Guid invitedByUserId, CancellationToken ct = default);
    Task<ActivateResponse> ActivateAccountAsync(ActivateRequest request, CancellationToken ct = default);

    /// <summary>Generates a new TOTP secret and QR URI. Does NOT persist anything yet.</summary>
    Task<MfaSetupResponse> SetupMfaAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Validates the OTP against the provided secret (from setup), then encrypts and persists
    /// the secret and sets mfa_enabled = true.
    /// </summary>
    Task EnableMfaAsync(Guid userId, MfaEnableRequest request, CancellationToken ct = default);

    /// <summary>Validates the OTP against the user's stored encrypted secret during login.</summary>
    Task<string> VerifyMfaLoginAsync(Guid userId, MfaLoginRequest request, CancellationToken ct = default);

    /// <summary>Owner-only: enable or disable MFA for any user in the household.</summary>
    Task AdminToggleMfaAsync(Guid targetUserId, bool enabled, CancellationToken ct = default);

    /// <summary>Owner-only: list all household members.</summary>
    Task<IReadOnlyList<UserSummaryResponse>> ListUsersAsync(CancellationToken ct = default);
}
