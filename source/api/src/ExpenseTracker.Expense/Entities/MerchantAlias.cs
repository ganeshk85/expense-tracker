namespace ExpenseTracker.Expense.Entities;

public sealed class MerchantAlias
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public string AliasNormalized { get; set; } = string.Empty;
    public string CanonicalNormalized { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
