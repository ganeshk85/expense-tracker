using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Auth.Models;

public sealed record LoginRequest(
    [property: Required, MaxLength(100)] string Username,
    [property: Required, MaxLength(200)] string Password);

public sealed record LoginResponse(Guid UserId, string Username, string Role, bool MfaRequired);

public sealed record InviteRequest(
    [property: Required, MaxLength(100)] string Username,
    [property: Required] string Role);

public sealed record InviteResponse(string Token, DateTimeOffset ExpiresAt);

public sealed record ActivateRequest(
    [property: Required] string Token,
    [property: Required, MinLength(8), MaxLength(200)] string Password);

public sealed record ActivateResponse(Guid UserId, string Username, string Role);

/// <summary>
/// Returned by POST /auth/mfa/setup. The secret is NOT persisted until
/// POST /auth/mfa/verify is called with a valid OTP.
/// </summary>
public sealed record MfaSetupResponse(string Secret, string OtpAuthUri);

/// <summary>
/// Body for POST /auth/mfa/verify — enables MFA by validating the generated secret + OTP pair.
/// </summary>
public sealed record MfaEnableRequest(
    [property: Required, MinLength(32), MaxLength(64)] string Secret,
    [property: Required, StringLength(6, MinimumLength = 6)] string Code);

/// <summary>
/// Body for POST /auth/mfa/login — completes login when MFA is pending in the session.
/// </summary>
public sealed record MfaLoginRequest(
    [property: Required, StringLength(6, MinimumLength = 6)] string Code);

/// <summary>
/// Body for PATCH /admin/users/{id}/mfa — Owner-only toggle.
/// </summary>
public sealed record AdminMfaToggleRequest(bool Enabled);

/// <summary>
/// Returned by GET /auth/session — current session userId and role.
/// </summary>
public sealed record SessionResponse(string UserId, string Role);

/// <summary>
/// Returned by GET /admin/users — household member summary (Owner-only).
/// </summary>
public sealed record UserSummaryResponse(
    Guid Id,
    string Username,
    string Role,
    bool IsActive,
    bool MfaEnabled,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);
