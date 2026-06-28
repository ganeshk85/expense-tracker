namespace ExpenseTracker.Budget.Entities;

public sealed class Notification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Type { get; set; }
    public required string Message { get; set; }
    public Guid? BudgetId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DismissedAt { get; set; }
}

public static class NotificationType
{
    public const string BudgetThreshold = "budget_threshold";
    public const string BudgetExceeded = "budget_exceeded";
    public const string BudgetDeleted = "budget_deleted";
}
