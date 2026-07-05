using ExpenseTracker.Expense.Models;

namespace ExpenseTracker.Expense.Services;

public interface IIntelligenceService
{
    Task<string?> GetSuggestedCategoryAsync(
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
}
