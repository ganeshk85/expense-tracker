using ExpenseTracker.Receipt.Models;
using Microsoft.AspNetCore.Http;

namespace ExpenseTracker.Receipt.Services;

public interface IReceiptService
{
    Task<UploadReceiptResponse> UploadAsync(IFormFile file, Guid userId, CancellationToken ct = default);
    /// <param name="requestingUserId">The user making the request — used to enforce ownership (403 if mismatch).</param>
    Task<ReceiptStatusResponse> GetStatusAsync(Guid receiptId, Guid requestingUserId, CancellationToken ct = default);
    Task UpdateThumbnailAsync(Guid receiptId, UpdateThumbnailRequest request, CancellationToken ct = default);
    Task<ThumbnailFileResult> GetThumbnailAsync(Guid receiptId, Guid requestingUserId, CancellationToken ct = default);
}
