namespace ExpenseTracker.Expense.Entities;

public sealed class DuplicateDismissal
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExpenseId { get; set; }
    public Guid DismissedBy { get; set; }
    public DateTimeOffset DismissedAt { get; init; } = DateTimeOffset.UtcNow;
}
