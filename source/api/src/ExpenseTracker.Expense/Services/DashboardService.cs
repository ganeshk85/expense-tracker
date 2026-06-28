using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Expense.Services;

public sealed class DashboardService(
    IExpenseManagementRepository repo,
    ILogger<DashboardService> logger) : IDashboardService
{
    private const string AdminRole = "Admin";
    private const int TopMerchantsCount = 5;

    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid userId, string userRole, string month, bool household, CancellationToken ct = default)
    {
        if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var parsedMonth))
            throw new ValidationException("Month must be in YYYY-MM format.");

        if (household && userRole != AdminRole)
            throw new ForbiddenException("Only an Owner can view the household dashboard.");

        Guid? filterUserId = household ? null : userId;

        var (totalSpent, expenseCount, byCategory, topMerchants) =
            await repo.GetDashboardDataAsync(filterUserId, parsedMonth, TopMerchantsCount, ct);

        decimal totalForPct = byCategory.Sum(x => x.Amount);

        var breakdown = byCategory
            .Select(x => new CategoryBreakdownItem(
                x.Category,
                x.Amount,
                totalForPct > 0 ? Math.Round(x.Amount / totalForPct * 100m, 2) : 0m))
            .ToList()
            .AsReadOnly();

        var merchants = topMerchants
            .Select(x => new TopMerchantItem(x.Merchant, x.TotalSpent, x.VisitCount))
            .ToList()
            .AsReadOnly();

        logger.LogInformation("Dashboard summary for user {UserId} month {Month} household={Household}", userId, month, household);

        return new DashboardSummaryResponse(month, totalSpent, expenseCount, breakdown, merchants);
    }
}
