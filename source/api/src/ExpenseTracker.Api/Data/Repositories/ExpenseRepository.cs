using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Ocr.Repositories;
using Microsoft.EntityFrameworkCore;
using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;

namespace ExpenseTracker.Api.Data.Repositories;

internal sealed class ExpenseRepository(AppDbContext db)
    : IExpenseRepository, IExpenseManagementRepository
{
    // ── IExpenseRepository (used by OcrResultConsumerService) ────────────────────

    public Task<ExpenseEntity?> FindByReceiptIdAsync(Guid receiptId, CancellationToken ct = default)
        => db.Expenses
              .Include(e => e.Items)
              .FirstOrDefaultAsync(e => e.ReceiptId == receiptId, ct);

    public async Task UpsertAsync(ExpenseEntity expense, CancellationToken ct = default)
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
            db.Entry(existing).CurrentValues.SetValues(expense);
            db.ExpenseItems.RemoveRange(existing.Items);
            existing.Items = expense.Items;
        }
    }

    // ── IExpenseManagementRepository (used by ExpenseService) ───────────────────

    public Task<ExpenseEntity?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Expenses
              .Include(e => e.Items)
              .AsNoTracking()
              .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<(IReadOnlyList<ExpenseEntity> Items, int Total)> ListAsync(
        Guid? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Expenses
            .Include(e => e.Items)
            .AsNoTracking();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.Date ?? e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), total);
    }

    public async Task AddAsync(ExpenseEntity expense, CancellationToken ct = default)
        => await db.Expenses.AddAsync(expense, ct);

    public Task DeleteAsync(ExpenseEntity expense, CancellationToken ct = default)
    {
        // FindByIdAsync uses AsNoTracking, so attach before removing.
        db.Expenses.Remove(db.Expenses.Attach(expense).Entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
