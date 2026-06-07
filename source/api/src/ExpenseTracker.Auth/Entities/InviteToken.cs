using ExpenseTracker.Shared.Entities;

namespace ExpenseTracker.Auth.Entities;

public sealed class InviteToken : BaseEntity
{
    public required string Token { get; set; }
    public required string InvitedUsername { get; set; }
    public required UserRole AssignedRole { get; set; }
    public required Guid InvitedByUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(48);
    public bool IsUsed { get; set; } = false;
}
