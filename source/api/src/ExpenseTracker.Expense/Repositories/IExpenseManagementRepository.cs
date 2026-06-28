using ExpenseAttachmentEntity = ExpenseTracker.Ocr.Entities.ExpenseAttachment;
using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;
using ExpenseItemEntity = ExpenseTracker.Ocr.Entities.ExpenseItem;
using ExpenseShareEntity = ExpenseTracker.Ocr.Entities.ExpenseShare;
using ReceiptEntity = ExpenseTracker.Receipt.Entities.Receipt;

namespace ExpenseTracker.Expense.Repositories;

public interface IExpenseManagementRepository
{
    // ── Expense ──────────────────────────────────────────────────────────────

    /// <summary>Read-only lookup (AsNoTracking). Use for GET responses only.</summary>
    Task<ExpenseEntity?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tracked lookup. Use for update and correction operations so SaveChanges persists changes.</summary>
    Task<ExpenseEntity?> FindByIdTrackedAsync(Guid id, CancellationToken ct = default);

    /// <param name="userId">When null, returns expenses for all users (Owner view).</param>
    /// <param name="userRole">Used to filter shared expenses for Contributor role.</param>
    Task<(IReadOnlyList<ExpenseEntity> Items, int Total)> ListAsync(
        Guid? userId, string? userRole, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(ExpenseEntity expense, CancellationToken ct = default);
    Task DeleteAsync(ExpenseEntity expense, CancellationToken ct = default);

    // ── Expense Items ────────────────────────────────────────────────────────

    Task<IReadOnlyList<ExpenseItemEntity>> GetItemsByExpenseIdAsync(Guid expenseId, CancellationToken ct = default);

    Task<ExpenseItemEntity?> FindItemByIdTrackedAsync(Guid itemId, Guid expenseId, CancellationToken ct = default);

    Task AddItemAsync(ExpenseItemEntity item, CancellationToken ct = default);

    Task RemoveItemAsync(ExpenseItemEntity item, CancellationToken ct = default);

    // ── Expense Shares ───────────────────────────────────────────────────────

    Task ReplaceSharesAsync(Guid expenseId, IReadOnlyList<ExpenseShareEntity> shares, CancellationToken ct = default);

    // ── Receipts (for expense gallery) ──────────────────────────────────────

    Task<IReadOnlyList<ReceiptEntity>> GetReceiptsByExpenseIdAsync(Guid expenseId, CancellationToken ct = default);

    Task<ReceiptEntity?> FindReceiptByIdTrackedAsync(Guid receiptId, CancellationToken ct = default);

    // ── Attachments ──────────────────────────────────────────────────────────

    Task<IReadOnlyList<ExpenseAttachmentEntity>> GetAttachmentsByExpenseIdAsync(Guid expenseId, CancellationToken ct = default);
    Task<ExpenseAttachmentEntity?> FindAttachmentByIdAsync(Guid attachmentId, Guid expenseId, CancellationToken ct = default);
    Task AddAttachmentAsync(ExpenseAttachmentEntity attachment, CancellationToken ct = default);
    Task RemoveAttachmentAsync(ExpenseAttachmentEntity attachment, CancellationToken ct = default);

    // ── Search ───────────────────────────────────────────────────────────────

    Task<(IReadOnlyList<ExpenseEntity> Items, int Total)> SearchAsync(
        Guid? userId, string? userRole,
        string? q, string? category, string? merchant,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        decimal? minAmount, decimal? maxAmount, string[]? tags,
        int page, int pageSize, CancellationToken ct = default);

    // ── Dashboard / Analytics ────────────────────────────────────────────────

    Task<(decimal TotalSpent, int ExpenseCount, IReadOnlyList<(string Category, decimal Amount)> ByCategory, IReadOnlyList<(string Merchant, decimal TotalSpent, int VisitCount)> TopMerchants)>
        GetDashboardDataAsync(Guid? userId, DateOnly month, int topMerchantsCount, CancellationToken ct = default);

    Task<IReadOnlyList<(string Month, string Category, decimal Amount)>> GetCategoryTrendsAsync(
        Guid? userId, int months, string? category, CancellationToken ct = default);

    Task<IReadOnlyList<(string Merchant, decimal TotalSpent, int VisitCount)>> GetMerchantRankingsAsync(
        Guid? userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);

    Task<IReadOnlyList<ExpenseEntity>> GetMerchantDetailAsync(
        Guid? userId, string merchantName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);

    // ── Export ───────────────────────────────────────────────────────────────

    Task<IReadOnlyList<ExpenseEntity>> GetForExportAsync(
        Guid? userId, string? userRole, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);

    // ── Shared ───────────────────────────────────────────────────────────────

    Task SaveChangesAsync(CancellationToken ct = default);
}
