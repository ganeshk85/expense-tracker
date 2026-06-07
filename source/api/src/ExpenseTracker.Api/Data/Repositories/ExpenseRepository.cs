using ExpenseTracker.Ocr.Entities;
using ExpenseTracker.Ocr.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data.Repositories;

internal sealed class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public Task<Expense?> FindByReceiptIdAsync(Guid receiptId, CancellationToken ct = default)
        => db.Expenses
              .Include(e => e.Items)
              .FirstOrDefaultAsync(e => e.ReceiptId == receiptId, ct);

    public async Task UpsertAsync(Expense expense, CancellationToken ct = default)
    {
        var existing = await db.Expenses
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.ReceiptId == expense.ReceiptId, ct);

        if (existing is null)
        {
            await db.Expenses.AddAsync(expense, ct);
        }
        else
        {
            // Detach the passed-in entity and work with the tracked one.
            db.Entry(existing).CurrentValues.SetValues(expense);

            // Replace line items.
            db.ExpenseItems.RemoveRange(existing.Items);
            existing.Items = expense.Items;
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
