using ExpenseTracker.Ocr.Entities;

namespace ExpenseTracker.Ocr.Repositories;

public interface IExpenseRepository
{
    Task<Expense?> FindByReceiptIdAsync(Guid receiptId, CancellationToken ct = default);
    Task UpsertAsync(Expense expense, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
