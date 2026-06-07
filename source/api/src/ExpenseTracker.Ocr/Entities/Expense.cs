namespace ExpenseTracker.Ocr.Entities;

/// <summary>
/// Expense record hydrated from OCR output.
/// Full CRUD (categories, tags, notes, shared) is Sprint 3.
/// This entity captures only the fields the OCR worker can extract.
/// </summary>
public sealed class Expense
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>FK to receipts table. One-to-one for OCR source.</summary>
    public Guid ReceiptId { get; set; }

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

    /// <summary>OCR processing state: "processing" | "complete" | "ocr_failed".</summary>
    public string OcrStatus { get; set; } = OcrStatusValue.Processing;

    /// <summary>Always "OCR" for records created by this worker. "Manual" for Sprint 3.</summary>
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
}

public static class ExpenseSource
{
    public const string Ocr = "OCR";
    public const string Manual = "Manual";
}
