namespace ExpenseTracker.Expense.Entities;

public sealed class OcrFieldAccuracy
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string MerchantNameNormalized { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public int TotalExtractions { get; set; }
    public int TotalCorrections { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}
