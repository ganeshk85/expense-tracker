namespace ExpenseTracker.Ocr.Entities;

public sealed class Expense
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Null for manually-created expenses that have no associated receipt.</summary>
    public Guid? ReceiptId { get; set; }

    public Guid UserId { get; set; }

    public string? MerchantName { get; set; }
    public string? MerchantAddress { get; set; }

    /// <summary>Receipt date parsed to UTC midnight. Null if OCR could not extract.</summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>Raw time string as printed on receipt (e.g. "14:32").</summary>
    public string? Time { get; set; }

    public decimal? Subtotal { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Total { get; set; }

    public string? Barcode { get; set; }

    public string? Category { get; set; }
    public string[] Tags { get; set; } = [];
    public string? Notes { get; set; }

    /// <summary>
    /// OCR per-field confidence as JSON object: {"merchantName":85,"date":90,"total":85}.
    /// Null for manually-created expenses. Not shown after user confirms the expense.
    /// </summary>
    public string? ConfidenceJson { get; set; }

    /// <summary>OCR processing state: "processing" | "complete" | "ocr_failed" | "manual".</summary>
    public string OcrStatus { get; set; } = OcrStatusValue.Processing;

    public string Source { get; set; } = ExpenseSource.Ocr;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ExpenseItem> Items { get; set; } = [];
}

public static class OcrStatusValue
{
    public const string Processing = "processing";
    public const string Complete = "complete";
    public const string OcrFailed = "ocr_failed";
    public const string Manual = "manual";
}

public static class ExpenseSource
{
    public const string Ocr = "OCR";
    public const string Manual = "Manual";
}
