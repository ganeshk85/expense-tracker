using ExpenseTracker.Budget.Models;

namespace ExpenseTracker.Budget.Services;

public interface IBudgetService
{
    Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, Guid userId, string userRole, CancellationToken ct = default);
    Task<BudgetListResponse> ListAsync(Guid userId, string userRole, CancellationToken ct = default);
    Task<BudgetResponse> UpdateAsync(Guid id, UpdateBudgetRequest request, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<BudgetHistoryListResponse> GetHistoryAsync(Guid userId, string month, CancellationToken ct = default);
    Task CheckAndFireAlertsAsync(Guid userId, string? category, CancellationToken ct = default);
    Task<NotificationListResponse> GetNotificationsAsync(Guid userId, CancellationToken ct = default);
    Task DismissNotificationAsync(Guid id, Guid userId, CancellationToken ct = default);
}
