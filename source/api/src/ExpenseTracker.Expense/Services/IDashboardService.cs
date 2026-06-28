using ExpenseTracker.Expense.Models;

namespace ExpenseTracker.Expense.Services;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        Guid userId, string userRole, string month, bool household, CancellationToken ct = default);
}
