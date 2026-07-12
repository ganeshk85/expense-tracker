using ExpenseTracker.Expense.Models;

namespace ExpenseTracker.Expense.Services;

public interface IIntelligenceService
{
    Task<CategorySuggestion?> GetSuggestedCategoryAsync(
        Guid householdId, string? merchantName, CancellationToken ct = default);

    Task<DuplicateWarning?> CheckDuplicateAsync(
        Guid householdId, string? merchantName, decimal? amount,
        DateTimeOffset? expenseDate, Guid excludeExpenseId, CancellationToken ct = default);

    Task DismissDuplicateAsync(
        Guid expenseId, Guid userId, CancellationToken ct = default);

    Task<TagSuggestionsResponse> GetTagSuggestionsAsync(
        Guid householdId, string? merchantName, CancellationToken ct = default);

    Task<MerchantCategoryMapResponse> GetMerchantCategoryMapAsync(
        Guid householdId, CancellationToken ct = default);

    Task<OcrAccuracyResponse> GetOcrAccuracyAsync(CancellationToken ct = default);

    // ── Merchant field templates (US-INT-05) ──────────────────────────────────

    Task UpsertMerchantTemplateAsync(
        Guid householdId, UpsertMerchantTemplateRequest request, CancellationToken ct = default);

    Task<MerchantFieldTemplatesResponse> GetMerchantTemplatesAsync(
        Guid householdId, CancellationToken ct = default);

    Task<MerchantFieldTemplatesResponse> GetMerchantTemplatesForMerchantAsync(
        Guid householdId, string merchantName, CancellationToken ct = default);

    Task<int> DeleteMerchantTemplatesAsync(
        Guid householdId, string merchantName, CancellationToken ct = default);

    // ── Recurring expenses (US-INT-06) ─────────────────────────────────────────

    Task<RecurringExpensesResponse> GetRecurringExpensesAsync(
        Guid householdId, CancellationToken ct = default);

    Task SnoozeRecurringExpenseAsync(
        Guid householdId, Guid id, int days, CancellationToken ct = default);

    Task DetectRecurringExpensesAsync(Guid householdId, CancellationToken ct = default);

    // ── Merchant aliases (US-INT-07) ───────────────────────────────────────────

    Task<MerchantAliasEntry> CreateAliasAsync(
        Guid householdId, CreateMerchantAliasRequest request, Guid createdBy, CancellationToken ct = default);

    Task<MerchantAliasesResponse> GetAliasesAsync(Guid householdId, CancellationToken ct = default);

    Task DeleteAliasAsync(Guid householdId, Guid id, CancellationToken ct = default);

    // ── Intelligence summary (US-INT-08) ───────────────────────────────────────

    Task<IntelligenceSummaryResponse> GetSummaryAsync(Guid householdId, CancellationToken ct = default);
}
