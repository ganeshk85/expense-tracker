using ExpenseTracker.Expense.Models;

namespace ExpenseTracker.Expense.Services;

public interface IAnalyticsService
{
    Task<CategoryTrendResponse> GetCategoryTrendsAsync(
        Guid userId, string userRole, int months, string? category, CancellationToken ct = default);

    Task<MerchantRankingsResponse> GetMerchantRankingsAsync(
        Guid userId, string userRole, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);

    Task<MerchantDetailResponse> GetMerchantDetailAsync(
        Guid userId, string userRole, string merchantName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
}
