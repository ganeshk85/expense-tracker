using ExpenseTracker.Shared.Entities;

namespace ExpenseTracker.Receipt.Entities;

public sealed class Receipt : BaseEntity
{
    public required Guid UploadedByUserId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StoragePath { get; set; }
    public required string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ThumbnailPath { get; set; }
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Uploaded;
    public int OcrRetryCount { get; set; } = 0;
}

public enum ReceiptStatus
{
    Uploaded,
    Processing,
    Complete,
    OcrFailed
}
