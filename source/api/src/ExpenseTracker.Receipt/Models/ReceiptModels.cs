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
    string? ThumbnailUrl,
    string? ImageQuality);

public sealed record UpdateThumbnailRequest(string ThumbnailPath);

public sealed record ThumbnailFileResult(string FilePath, string ContentType);
