using ExpenseTracker.Expense.Entities;

namespace ExpenseTracker.Expense.Repositories;

public interface IIntelligenceRepository
{
    // ── Merchant-category map ─────────────────────────────────────────────────

    Task<MerchantCategoryMap?> FindMerchantCategoryAsync(
        Guid householdId, string merchantNormalized, CancellationToken ct = default);

    Task UpsertMerchantCategoryAsync(
        Guid householdId, string merchantNormalized, string category, CancellationToken ct = default);

    Task<IReadOnlyList<MerchantCategoryMap>> GetMerchantCategoryMapAsync(
        Guid householdId, CancellationToken ct = default);

    // ── Duplicate detection ───────────────────────────────────────────────────

    Task<(Guid ExpenseId, DateTimeOffset? Date)?> FindPotentialDuplicateAsync(
        Guid householdId, string merchantNormalized, decimal amount,
        DateTimeOffset expenseDate, CancellationToken ct = default);

    Task<bool> IsDismissedAsync(Guid expenseId, CancellationToken ct = default);

    Task DismissDuplicateAsync(Guid expenseId, Guid dismissedBy, CancellationToken ct = default);

    // ── Tag history ───────────────────────────────────────────────────────────

    Task<IReadOnlyList<string>> GetTagSuggestionsAsync(
        Guid householdId, string merchantNormalized, int maxResults, CancellationToken ct = default);

    Task UpsertTagHistoryAsync(
        Guid householdId, string merchantNormalized, string[] tags, CancellationToken ct = default);

    // ── OCR accuracy ──────────────────────────────────────────────────────────

    Task UpsertOcrFieldAccuracyAsync(
        string merchantNormalized, string fieldName, CancellationToken ct = default);

    Task<IReadOnlyList<OcrFieldAccuracy>> GetOcrAccuracyAsync(CancellationToken ct = default);

    // ── Household ID resolution ───────────────────────────────────────────────

    Task<Guid> GetHouseholdIdForUserAsync(Guid userId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
