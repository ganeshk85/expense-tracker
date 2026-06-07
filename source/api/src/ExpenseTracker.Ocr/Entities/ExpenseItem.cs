namespace ExpenseTracker.Ocr.Entities;

/// <summary>Line-item extracted from a receipt by the OCR worker.</summary>
public sealed class ExpenseItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExpenseId { get; set; }
    public required string Name { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
