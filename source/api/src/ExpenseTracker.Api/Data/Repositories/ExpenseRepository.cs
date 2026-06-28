using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Ocr.Repositories;
using Microsoft.EntityFrameworkCore;
using ExpenseAttachmentEntity = ExpenseTracker.Ocr.Entities.ExpenseAttachment;
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

    // ── IExpenseManagementRepository — Attachments ──────────────────────────

    public async Task<IReadOnlyList<ExpenseAttachmentEntity>> GetAttachmentsByExpenseIdAsync(
        Guid expenseId, CancellationToken ct = default)
        => await db.ExpenseAttachments
                   .AsNoTracking()
                   .Where(a => a.ExpenseId == expenseId)
                   .OrderBy(a => a.CreatedAt)
                   .ToListAsync(ct);

    public Task<ExpenseAttachmentEntity?> FindAttachmentByIdAsync(
        Guid attachmentId, Guid expenseId, CancellationToken ct = default)
        => db.ExpenseAttachments
              .FirstOrDefaultAsync(a => a.Id == attachmentId && a.ExpenseId == expenseId, ct);

    public async Task AddAttachmentAsync(ExpenseAttachmentEntity attachment, CancellationToken ct = default)
        => await db.ExpenseAttachments.AddAsync(attachment, ct);

    public Task RemoveAttachmentAsync(ExpenseAttachmentEntity attachment, CancellationToken ct = default)
    {
        db.ExpenseAttachments.Remove(attachment);
        return Task.CompletedTask;
    }

    // ── IExpenseManagementRepository — Search ────────────────────────────────

    public async Task<(IReadOnlyList<ExpenseEntity> Items, int Total)> SearchAsync(
        Guid? userId, string? userRole,
        string? q, string? category, string? merchant,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        decimal? minAmount, decimal? maxAmount, string[]? tags,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Expenses
            .Include(e => e.Items)
            .Include(e => e.Shares)
            .AsNoTracking();

        // Visibility filter (mirrors ListAsync logic)
        if (userId.HasValue)
        {
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

        // Full-text filter (merchant name OR notes, case-insensitive)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var qLower = q.ToLower();
            query = query.Where(e =>
                (e.MerchantName != null && EF.Functions.ILike(e.MerchantName, $"%{qLower}%")) ||
                (e.Notes != null && EF.Functions.ILike(e.Notes, $"%{qLower}%")));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(merchant))
            query = query.Where(e => e.MerchantName != null &&
                EF.Functions.ILike(e.MerchantName, $"%{merchant}%"));

        if (dateFrom.HasValue)
            query = query.Where(e => e.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(e => e.Date <= dateTo.Value);

        if (minAmount.HasValue)
            query = query.Where(e => e.Total >= minAmount.Value);

        if (maxAmount.HasValue)
            query = query.Where(e => e.Total <= maxAmount.Value);

        if (tags is { Length: > 0 })
            query = query.Where(e => tags.All(t => e.Tags.Contains(t)));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.Date ?? e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items.AsReadOnly(), total);
    }

    // ── Dashboard / Analytics ────────────────────────────────────────────────

    public async Task<(decimal TotalSpent, int ExpenseCount,
        IReadOnlyList<(string Category, decimal Amount)> ByCategory,
        IReadOnlyList<(string Merchant, decimal TotalSpent, int VisitCount)> TopMerchants)>
        GetDashboardDataAsync(Guid? userId, DateOnly month, int topMerchantsCount, CancellationToken ct = default)
    {
        var from = new DateTimeOffset(month.Year, month.Month, month.Day, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);

        var query = db.Expenses.AsNoTracking()
            .Where(e => e.Date >= from && e.Date < to);

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        var totalSpent = await query.Where(e => e.Total.HasValue).SumAsync(e => e.Total!.Value, ct);
        var expenseCount = await query.CountAsync(ct);

        var byCategory = await query
            .Where(e => e.Category != null && e.Total.HasValue)
            .GroupBy(e => e.Category!)
            .Select(g => new { Category = g.Key, Amount = g.Sum(e => e.Total!.Value) })
            .OrderByDescending(x => x.Amount)
            .ToListAsync(ct);

        var topMerchants = await query
            .Where(e => e.MerchantName != null && e.Total.HasValue)
            .GroupBy(e => e.MerchantName!)
            .Select(g => new { Merchant = g.Key, TotalSpent = g.Sum(e => e.Total!.Value), VisitCount = g.Count() })
            .OrderByDescending(x => x.TotalSpent)
            .Take(topMerchantsCount)
            .ToListAsync(ct);

        return (
            totalSpent,
            expenseCount,
            byCategory.Select(x => (x.Category, x.Amount)).ToList().AsReadOnly(),
            topMerchants.Select(x => (x.Merchant, x.TotalSpent, x.VisitCount)).ToList().AsReadOnly()
        );
    }

    // ── Export ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ExpenseEntity>> GetForExportAsync(
        Guid? userId, string? userRole, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var query = db.Expenses
            .Include(e => e.Items)
            .Include(e => e.Shares)
            .AsNoTracking();

        if (userId.HasValue)
        {
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

        if (from.HasValue)
            query = query.Where(e => e.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.Date <= to.Value);

        return await query.OrderByDescending(e => e.Date ?? e.CreatedAt).ToListAsync(ct);
    }

    // ── Shared ───────────────────────────────────────────────────────────────

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
