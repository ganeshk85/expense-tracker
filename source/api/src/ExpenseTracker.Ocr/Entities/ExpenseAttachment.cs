namespace ExpenseTracker.Ocr.Entities;

public sealed class ExpenseAttachment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExpenseId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public required string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
