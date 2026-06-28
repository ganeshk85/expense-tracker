namespace ExpenseTracker.Ocr.Entities;

public sealed class ExpenseShare
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExpenseId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Fixed split amount. Null when split is expressed as a percentage.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Split as percentage of total (0–100). Null when split is a fixed amount.</summary>
    public decimal? Percentage { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
