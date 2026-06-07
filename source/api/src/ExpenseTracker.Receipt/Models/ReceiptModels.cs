namespace ExpenseTracker.Receipt.Models;

public sealed record UploadReceiptResponse(
    Guid ReceiptId,
    string Status,
    string? ThumbnailUrl,
    DateTimeOffset UploadedAt);

public sealed record ReceiptStatusResponse(
    Guid ReceiptId,
    string Status,
    int OcrRetryCount,
    string? ThumbnailUrl);

public sealed record UpdateThumbnailRequest(string ThumbnailPath);
