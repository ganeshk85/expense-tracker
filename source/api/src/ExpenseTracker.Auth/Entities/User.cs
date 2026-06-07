using ExpenseTracker.Shared.Entities;

namespace ExpenseTracker.Auth.Entities;

public sealed class User : BaseEntity
{
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MfaEnabled { get; set; } = false;
    public string? TotpSecretEncrypted { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}

public enum UserRole
{
    Owner,
    AdultMember,
    RestrictedMember
}
