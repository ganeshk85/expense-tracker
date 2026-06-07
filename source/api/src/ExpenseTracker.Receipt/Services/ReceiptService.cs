using ExpenseTracker.Receipt.Entities;
using ExpenseTracker.Receipt.Models;
using ExpenseTracker.Receipt.Repositories;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace ExpenseTracker.Receipt.Services;

public sealed class ReceiptService(
    IReceiptRepository receipts,
    IConnectionMultiplexer redis,
    IOptions<StorageOptions> storageOptions,
    ILogger<ReceiptService> logger) : IReceiptService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/heic", "image/heif", "application/pdf"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<UploadReceiptResponse> UploadAsync(IFormFile file, Guid userId, CancellationToken ct = default)
    {
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new ValidationException($"Unsupported file type '{file.ContentType}'. Accepted formats: JPG, PNG, HEIC, PDF.");

        if (file.Length > MaxFileSizeBytes)
            throw new ValidationException("File too large. Maximum size is 10 MB.");

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var userDir = Path.Combine(storageOptions.Value.ReceiptsPath, userId.ToString());
        Directory.CreateDirectory(userDir);
        var fullPath = Path.Combine(userDir, storedFileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var receipt = new Entities.Receipt
        {
            UploadedByUserId = userId,
            OriginalFileName = file.FileName,
            StoragePath = fullPath,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        };

        await receipts.AddAsync(receipt, ct);
        await receipts.SaveChangesAsync(ct);

        await EnqueueThumbnailJobAsync(receipt.Id, fullPath);
        await EnqueueOcrJobAsync(receipt.Id, fullPath, userId);

        // Mark receipt as processing after enqueue.
        receipt.Status = ReceiptStatus.Processing;
        receipt.UpdatedAt = DateTimeOffset.UtcNow;
        await receipts.SaveChangesAsync(ct);

        logger.LogInformation("Receipt {ReceiptId} uploaded by user {UserId}; OCR job enqueued", receipt.Id, userId);
        return new UploadReceiptResponse(receipt.Id, receipt.Status.ToString(), null, receipt.CreatedAt);
    }

    public async Task<ReceiptStatusResponse> GetStatusAsync(
        Guid receiptId,
        Guid requestingUserId,
        CancellationToken ct = default)
    {
        var receipt = await receipts.FindByIdAsync(receiptId, ct)
            ?? throw new NotFoundException("Receipt", receiptId);

        if (receipt.UploadedByUserId != requestingUserId)
            throw new ForbiddenException("You do not have access to this receipt.");

        var thumbnailUrl = receipt.ThumbnailPath is not null
            ? $"/receipts/{receipt.Id}/thumbnail"
            : null;

        return new ReceiptStatusResponse(receipt.Id, receipt.Status.ToString(), receipt.OcrRetryCount, thumbnailUrl);
    }

    public async Task UpdateThumbnailAsync(Guid receiptId, UpdateThumbnailRequest request, CancellationToken ct = default)
    {
        var receipt = await receipts.FindByIdAsync(receiptId, ct)
            ?? throw new NotFoundException("Receipt", receiptId);

        receipt.ThumbnailPath = request.ThumbnailPath;
        receipt.UpdatedAt = DateTimeOffset.UtcNow;
        await receipts.SaveChangesAsync(ct);
    }

    private async Task EnqueueThumbnailJobAsync(Guid receiptId, string filePath)
    {
        var db = redis.GetDatabase();
        var payload = JsonSerializer.Serialize(new { receiptId, filePath });
        await db.ListLeftPushAsync("receipt.uploaded", payload);
        logger.LogDebug("Enqueued thumbnail job for receipt {ReceiptId}", receiptId);
    }

    private async Task EnqueueOcrJobAsync(Guid receiptId, string filePath, Guid userId)
    {
        var db = redis.GetDatabase();

        // Publish to Redis stream ocr.jobs using XADD.
        // The payload field carries the full JSON blob so the OCR worker only needs to
        // parse one field, mirroring the thumbnail worker's pattern.
        var payload = JsonSerializer.Serialize(new
        {
            receiptId = receiptId.ToString(),
            filePath,
            userId = userId.ToString(),
            submittedAt = DateTimeOffset.UtcNow.ToString("O"),
        });

        await db.StreamAddAsync(
            "ocr.jobs",
            new NameValueEntry[] { new("payload", payload) });

        logger.LogDebug("Enqueued OCR job for receipt {ReceiptId}", receiptId);
    }
}

public sealed class StorageOptions
{
    public string ReceiptsPath { get; set; } = "/storage/receipts";
    public string ThumbnailsPath { get; set; } = "/storage/thumbnails";
    public string OcrJsonPath { get; set; } = "/storage/ocr-json";
    public string AttachmentsPath { get; set; } = "/storage/attachments";
}
