namespace ExpenseTracker.Expense.Entities;

public sealed class MerchantTagHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public string MerchantNameNormalized { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int UseCount { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
