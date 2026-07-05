namespace ExpenseTracker.Expense.Entities;

public sealed class MerchantCategoryMap
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public string MerchantNameNormalized { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ConfirmedCount { get; set; }
    public DateTimeOffset LastConfirmedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
