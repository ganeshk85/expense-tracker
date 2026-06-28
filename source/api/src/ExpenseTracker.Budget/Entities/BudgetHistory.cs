namespace ExpenseTracker.Budget.Entities;

public sealed class BudgetHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public DateOnly Month { get; set; }
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
