using ExpenseTracker.Api.Data;
using ExpenseTracker.Receipt.Repositories;
using Microsoft.EntityFrameworkCore;
using ReceiptEntity = ExpenseTracker.Receipt.Entities.Receipt;

namespace ExpenseTracker.Api.Data.Repositories;

internal sealed class ReceiptRepository(AppDbContext db) : IReceiptRepository
{
    public Task<ReceiptEntity?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Receipts.FindAsync([id], ct).AsTask();

    public async Task AddAsync(ReceiptEntity receipt, CancellationToken ct = default)
        => await db.Receipts.AddAsync(receipt, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
