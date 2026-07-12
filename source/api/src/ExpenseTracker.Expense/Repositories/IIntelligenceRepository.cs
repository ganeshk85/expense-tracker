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
        string merchantNormalized, string fieldName, bool isCorrected, CancellationToken ct = default);

    Task<IReadOnlyList<OcrFieldAccuracy>> GetOcrAccuracyAsync(CancellationToken ct = default);

    // ── Household ID resolution ───────────────────────────────────────────────

    Task<Guid> GetHouseholdIdForUserAsync(Guid userId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    // ── Merchant field templates (US-INT-05) ──────────────────────────────────

    Task UpsertMerchantTemplateAsync(
        Guid householdId, string merchantNormalized, string fieldName,
        double regionX, double regionY, double regionW, double regionH, CancellationToken ct = default);

    Task<IReadOnlyList<MerchantFieldTemplate>> GetMerchantTemplatesAsync(
        Guid householdId, CancellationToken ct = default);

    Task<MerchantFieldTemplate?> FindMerchantTemplateAsync(
        Guid householdId, string merchantNormalized, string fieldName, CancellationToken ct = default);

    /// <returns>The number of template rows removed.</returns>
    Task<int> DeleteMerchantTemplatesAsync(
        Guid householdId, string merchantNormalized, CancellationToken ct = default);

    // ── Recurring expenses (US-INT-06) ─────────────────────────────────────────

    Task<IReadOnlyList<RecurringExpense>> GetRecurringExpensesAsync(
        Guid householdId, CancellationToken ct = default);

    Task<RecurringExpense?> FindRecurringExpenseAsync(
        Guid householdId, Guid id, CancellationToken ct = default);

    Task SnoozeRecurringExpenseAsync(
        Guid householdId, Guid id, int days, CancellationToken ct = default);

    /// <summary>
    /// Scans the household's last 6 months of confirmed expenses for merchant+amount
    /// patterns appearing in at least 3 of the last 4 calendar months, and upserts
    /// the results into recurring_expenses.
    /// </summary>
    Task DetectRecurringExpensesAsync(Guid householdId, CancellationToken ct = default);

    // ── Merchant aliases (US-INT-07) ───────────────────────────────────────────

    /// <summary>Returns the canonical normalized name for a merchant, or the input unchanged if no alias exists.</summary>
    Task<string> ResolveAliasAsync(Guid householdId, string merchantNormalized, CancellationToken ct = default);

    Task<MerchantAlias> CreateAliasAsync(
        Guid householdId, string aliasNormalized, string canonicalNormalized, Guid createdBy, CancellationToken ct = default);

    Task<IReadOnlyList<MerchantAlias>> GetAliasesAsync(Guid householdId, CancellationToken ct = default);

    Task DeleteAliasAsync(Guid householdId, Guid id, CancellationToken ct = default);

    // ── Intelligence summary (US-INT-08) ───────────────────────────────────────

    Task<(int MerchantMappings, int FieldTemplates, int RecurringExpenses, int Aliases)> GetSummaryCountsAsync(
        Guid householdId, CancellationToken ct = default);
}
