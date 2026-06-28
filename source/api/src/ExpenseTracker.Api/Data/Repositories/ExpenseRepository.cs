using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Ocr.Repositories;
using Microsoft.EntityFrameworkCore;
using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;
using ExpenseItemEntity = ExpenseTracker.Ocr.Entities.ExpenseItem;
using ExpenseShareEntity = ExpenseTracker.Ocr.Entities.ExpenseShare;
using ReceiptEntity = ExpenseTracker.Receipt.Entities.Receipt;

namespace ExpenseTracker.Api.Data.Repositories;

internal sealed class ExpenseRepository(AppDbContext db)
    : IExpenseRepository, IExpenseManagementRepository
{
    private const string ContributorRole = "Contributor";

    // ── IExpenseRepository (used by OcrResultConsumerService) ────────────────────

    public Task<ExpenseEntity?> FindByReceiptIdAsync(Guid receiptId, CancellationToken ct = default)
        => db.Expenses
              .Include(e => e.Items)
              .Include(e => e.Shares)
              .FirstOrDefaultAsync(e => e.ReceiptId == receiptId, ct);

    public async Task UpsertAsync(ExpenseEntity expense, CancellationToken ct = default)
    {
        var existing = await db.Expenses
            .Include(e => e.Items)
            .Include(e => e.Shares)
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

    // ── IExpenseManagementRepository — Expense ──────────────────────────────

    public Task<ExpenseEntity?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Expenses
              .Include(e => e.Items)
              .Include(e => e.Shares)
              .AsNoTracking()
              .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<ExpenseEntity?> FindByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => db.Expenses
              .Include(e => e.Items)
              .Include(e => e.Shares)
              .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<(IReadOnlyList<ExpenseEntity> Items, int Total)> ListAsync(
        Guid? userId, string? userRole, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Expenses
            .Include(e => e.Items)
            .Include(e => e.Shares)
            .AsNoTracking();

        if (userId.HasValue)
        {
            // Contributor sees own expenses + shared expenses they are part of.
            // Reader sees only their own expenses.
            if (userRole == ContributorRole)
            {
                var uid = userId.Value;
                query = query.Where(e =>
                    e.UserId == uid ||
                    (e.IsShared && e.Shares.Any(s => s.UserId == uid)));
            }
            else
            {
                query = query.Where(e => e.UserId == userId.Value);
            }
        }

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

    // ── IExpenseManagementRepository — Items ────────────────────────────────

    public async Task<IReadOnlyList<ExpenseItemEntity>> GetItemsByExpenseIdAsync(
        Guid expenseId, CancellationToken ct = default)
    {
        return await db.ExpenseItems
            .AsNoTracking()
            .Where(i => i.ExpenseId == expenseId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<ExpenseItemEntity?> FindItemByIdTrackedAsync(
        Guid itemId, Guid expenseId, CancellationToken ct = default)
        => db.ExpenseItems
              .FirstOrDefaultAsync(i => i.Id == itemId && i.ExpenseId == expenseId, ct);

    public async Task AddItemAsync(ExpenseItemEntity item, CancellationToken ct = default)
        => await db.ExpenseItems.AddAsync(item, ct);

    public Task RemoveItemAsync(ExpenseItemEntity item, CancellationToken ct = default)
    {
        db.ExpenseItems.Remove(item);
        return Task.CompletedTask;
    }

    // ── IExpenseManagementRepository — Shares ────────────────────────────────

    public async Task ReplaceSharesAsync(
        Guid expenseId, IReadOnlyList<ExpenseShareEntity> shares, CancellationToken ct = default)
    {
        var existing = await db.ExpenseShares
            .Where(s => s.ExpenseId == expenseId)
            .ToListAsync(ct);

        db.ExpenseShares.RemoveRange(existing);
        await db.ExpenseShares.AddRangeAsync(shares, ct);
    }

    // ── IExpenseManagementRepository — Receipts ──────────────────────────────

    public async Task<IReadOnlyList<ReceiptEntity>> GetReceiptsByExpenseIdAsync(
        Guid expenseId, CancellationToken ct = default)
    {
        // Primary receipt: found via expense.ReceiptId.
        // Additional attached receipts: where Receipt.ExpenseId = expenseId.
        var expense = await db.Expenses
            .AsNoTracking()
            .Select(e => new { e.Id, e.ReceiptId })
            .FirstOrDefaultAsync(e => e.Id == expenseId, ct);

        if (expense is null) return [];

        return await db.Receipts
            .AsNoTracking()
            .Where(r => r.Id == expense.ReceiptId || r.ExpenseId == expenseId)
            .ToListAsync(ct);
    }

    public Task<ReceiptEntity?> FindReceiptByIdTrackedAsync(Guid receiptId, CancellationToken ct = default)
        => db.Receipts.FirstOrDefaultAsync(r => r.Id == receiptId, ct);

    // ── Shared ───────────────────────────────────────────────────────────────

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
