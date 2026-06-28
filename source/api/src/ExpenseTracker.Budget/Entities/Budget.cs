namespace ExpenseTracker.Budget.Entities;

public sealed class Budget
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Category { get; set; }
    public decimal MonthlyLimit { get; set; }
    public string Type { get; set; } = BudgetType.Category;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class BudgetType
{
    public const string Category = "category";
    public const string Household = "household";
}
