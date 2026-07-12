namespace ExpenseTracker.Expense.Entities;

public sealed class MerchantFieldTemplate
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid HouseholdId { get; set; }
    public string MerchantNameNormalized { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public double RegionX { get; set; }
    public double RegionY { get; set; }
    public double RegionW { get; set; }
    public double RegionH { get; set; }
    public int SampleCount { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}
