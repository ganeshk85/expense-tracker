namespace ExpenseTracker.Ocr.Models;

/// <summary>Redis stream names used by both the BE and OCR worker.</summary>
public static class OcrStreams
{
    public const string JobsStream = "ocr.jobs";
    public const string ResultsStream = "ocr.results";
    public const string ConsumerGroup = "api-consumer";
}

/// <summary>
/// Message published to <c>ocr.jobs</c> stream after a receipt is uploaded.
/// Field names are camelCase to match the Python worker's json.loads() expectations.
/// </summary>
public sealed record OcrJobMessage(
    string ReceiptId,
    string FilePath,
    string UserId,
    string SubmittedAt);

/// <summary>
/// Message consumed from <c>ocr.results</c> stream.
/// Maps 1:1 to the JSON schema the OCR worker pushes.
/// </summary>
public sealed record OcrResultMessage(
    string ReceiptId,
    string Status,
    string? MerchantName,
    string? MerchantAddress,
    string? Date,
    string? Time,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal? Total,
    IReadOnlyList<OcrLineItem>? LineItems,
    string? Barcode,
    string? BarcodeType,
    string? ImageQuality,
    IReadOnlyDictionary<string, int>? Confidence,
    string? RawOcrPath,
    string? ErrorMessage);

public sealed record OcrLineItem(
    string Name,
    decimal Quantity,
    decimal UnitPrice);

/// <summary>Status values published by the OCR worker.</summary>
public static class OcrResultStatus
{
    public const string Complete = "complete";
    public const string OcrFailed = "ocr_failed";
}
