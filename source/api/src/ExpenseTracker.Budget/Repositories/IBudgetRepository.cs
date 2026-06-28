using ExpenseTracker.Budget.Models;
using BudgetEntity = ExpenseTracker.Budget.Entities.Budget;
using BudgetHistoryEntity = ExpenseTracker.Budget.Entities.BudgetHistory;
using NotificationEntity = ExpenseTracker.Budget.Entities.Notification;

namespace ExpenseTracker.Budget.Repositories;

public interface IBudgetRepository
{
    // ── Budget CRUD ───────────────────────────────────────────────────────────

    Task<List<BudgetEntity>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<BudgetEntity>> ListAllAsync(CancellationToken ct = default);
    Task<BudgetEntity?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<BudgetEntity?> FindByUserAndCategoryAsync(Guid userId, string category, CancellationToken ct = default);
    Task<BudgetEntity?> FindHouseholdBudgetByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(BudgetEntity budget, CancellationToken ct = default);
    Task RemoveAsync(BudgetEntity budget, CancellationToken ct = default);

    // ── Spending calculation ──────────────────────────────────────────────────

    Task<decimal> GetCategorySpentAsync(Guid userId, string category, DateOnly month, CancellationToken ct = default);
    Task<decimal> GetHouseholdSpentAsync(DateOnly month, CancellationToken ct = default);
    Task<List<MemberContributionResponse>> GetMemberBreakdownAsync(DateOnly month, CancellationToken ct = default);

    // ── Owner lookup ──────────────────────────────────────────────────────────

    Task<Guid?> FindOwnerUserIdAsync(CancellationToken ct = default);

    // ── Notifications ─────────────────────────────────────────────────────────

    Task<NotificationEntity?> FindUndismissedAlertAsync(
        Guid userId, Guid budgetId, string type, DateOnly month, CancellationToken ct = default);
    Task AddNotificationAsync(NotificationEntity notification, CancellationToken ct = default);
    Task<NotificationEntity?> FindNotificationByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<List<NotificationEntity>> GetUnreadNotificationsAsync(Guid userId, CancellationToken ct = default);

    // ── Budget History ────────────────────────────────────────────────────────

    Task<bool> HasHistoryForMonthAsync(Guid budgetId, DateOnly month, CancellationToken ct = default);
    Task AddHistoryAsync(BudgetHistoryEntity history, CancellationToken ct = default);
    Task<List<BudgetHistoryEntity>> GetHistoryByUserAndMonthAsync(Guid userId, DateOnly month, CancellationToken ct = default);

    // ── Persistence ───────────────────────────────────────────────────────────

    Task SaveChangesAsync(CancellationToken ct = default);
}
