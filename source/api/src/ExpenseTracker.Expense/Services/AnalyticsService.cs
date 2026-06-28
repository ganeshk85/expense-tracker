using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Expense.Services;

public sealed class AnalyticsService(
    IExpenseManagementRepository repo,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    private const string AdminRole = "Admin";
    private const decimal SpikeThreshold = 0.20m;

    public async Task<CategoryTrendResponse> GetCategoryTrendsAsync(
        Guid userId, string userRole, int months, string? category, CancellationToken ct = default)
    {
        if (months is < 1 or > 24)
            throw new ValidationException("months must be between 1 and 24.");

        Guid? filterUserId = userRole == AdminRole ? null : userId;

        var raw = await repo.GetCategoryTrendsAsync(filterUserId, months, category, ct);

        var monthKeys = BuildMonthKeys(months);

        // Group raw results by category → month → amount for O(1) lookups below.
        var lookup = raw
            .GroupBy(x => x.Category)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.Month, x => x.Amount));

        // Sort categories by total spend descending.
        var orderedCategories = lookup.Keys
            .OrderByDescending(c => lookup[c].Values.Sum())
            .ToList();

        var series = orderedCategories.Select(cat =>
        {
            var byMonth = lookup[cat];
            var points = new List<CategoryMonthDataPoint>(monthKeys.Count);

            for (int i = 0; i < monthKeys.Count; i++)
            {
                var m = monthKeys[i];
                var amount = byMonth.TryGetValue(m, out var a) ? a : 0m;
                var prev = i > 0 && byMonth.TryGetValue(monthKeys[i - 1], out var p) ? p : 0m;
                bool spiked = prev > 0 && (amount - prev) / prev > SpikeThreshold;
                points.Add(new CategoryMonthDataPoint(m, amount, spiked));
            }

            return new CategoryTrendSeries(cat, points.AsReadOnly());
        }).ToList().AsReadOnly();

        logger.LogInformation("Category trends fetched for user {UserId} months={Months}", userId, months);

        return new CategoryTrendResponse(monthKeys.AsReadOnly(), series);
    }

    public async Task<MerchantRankingsResponse> GetMerchantRankingsAsync(
        Guid userId, string userRole, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        Guid? filterUserId = userRole == AdminRole ? null : userId;

        var raw = await repo.GetMerchantRankingsAsync(filterUserId, from, to, ct);

        var merchants = raw
            .Select(x => new MerchantRankItem(x.Merchant, x.TotalSpent, x.VisitCount))
            .ToList().AsReadOnly();

        logger.LogInformation("Merchant rankings fetched for user {UserId}", userId);

        return new MerchantRankingsResponse(merchants);
    }

    public async Task<MerchantDetailResponse> GetMerchantDetailAsync(
        Guid userId, string userRole, string merchantName, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            throw new ValidationException("Merchant name is required.");

        Guid? filterUserId = userRole == AdminRole ? null : userId;

        var expenses = await repo.GetMerchantDetailAsync(filterUserId, merchantName, from, to, ct);

        var totalSpent = expenses.Where(e => e.Total.HasValue).Sum(e => e.Total!.Value);
        var items = expenses
            .Select(e => new MerchantExpenseItem(
                e.Id.ToString(),
                e.Date.HasValue ? e.Date.Value.ToString("yyyy-MM-dd") : null,
                e.Total,
                e.Category,
                e.Notes))
            .ToList().AsReadOnly();

        logger.LogInformation("Merchant detail fetched for user {UserId} merchant={Merchant}", userId, merchantName);

        return new MerchantDetailResponse(merchantName, totalSpent, expenses.Count, items);
    }

    private static List<string> BuildMonthKeys(int months)
    {
        var now = DateTime.UtcNow;
        var keys = new List<string>(months);
        for (int i = months - 1; i >= 0; i--)
        {
            var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            keys.Add($"{d.Year:D4}-{d.Month:D2}");
        }
        return keys;
    }
}
