namespace ExpenseTracker.Expense.Entities;

public sealed class RecurringExpense
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public string MerchantNameNormalized { get; set; } = string.Empty;
    public decimal AverageAmount { get; set; }
    public int TypicalDayOfMonth { get; set; }
    public string Confidence { get; set; } = string.Empty; // "confirmed" | "likely"
    public DateTimeOffset LastDetectedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SnoozedUntil { get; set; }
}
