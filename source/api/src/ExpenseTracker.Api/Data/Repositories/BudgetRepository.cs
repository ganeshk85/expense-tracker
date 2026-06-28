using ExpenseTracker.Budget.Models;
using ExpenseTracker.Budget.Repositories;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = ExpenseTracker.Budget.Entities.Budget;
using BudgetHistoryEntity = ExpenseTracker.Budget.Entities.BudgetHistory;
using BudgetType = ExpenseTracker.Budget.Entities.BudgetType;
using NotificationEntity = ExpenseTracker.Budget.Entities.Notification;

namespace ExpenseTracker.Api.Data.Repositories;

public sealed class BudgetRepository(AppDbContext db) : IBudgetRepository
{
    // ── Budget CRUD ───────────────────────────────────────────────────────────

    public Task<List<BudgetEntity>> ListByUserAsync(Guid userId, CancellationToken ct = default)
        => db.Budgets.AsNoTracking().Where(b => b.UserId == userId).OrderBy(b => b.Category).ToListAsync(ct);

    public Task<List<BudgetEntity>> ListAllAsync(CancellationToken ct = default)
        => db.Budgets.AsNoTracking().ToListAsync(ct);

    public Task<BudgetEntity?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Budgets.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<BudgetEntity?> FindByUserAndCategoryAsync(Guid userId, string category, CancellationToken ct = default)
        => db.Budgets.FirstOrDefaultAsync(b => b.UserId == userId && b.Category == category, ct);

    public Task<BudgetEntity?> FindHouseholdBudgetByUserAsync(Guid userId, CancellationToken ct = default)
        => db.Budgets.FirstOrDefaultAsync(b => b.UserId == userId && b.Type == BudgetType.Household, ct);

    public async Task AddAsync(BudgetEntity budget, CancellationToken ct = default)
        => await db.Budgets.AddAsync(budget, ct);

    public Task RemoveAsync(BudgetEntity budget, CancellationToken ct = default)
    {
        db.Budgets.Remove(budget);
        return Task.CompletedTask;
    }

    // ── Spending calculation ──────────────────────────────────────────────────

    public Task<decimal> GetCategorySpentAsync(Guid userId, string category, DateOnly month, CancellationToken ct = default)
    {
        var from = new DateTimeOffset(month.Year, month.Month, month.Day, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);

        return db.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == userId
                && e.Category == category
                && e.Date >= from
                && e.Date < to
                && e.Total.HasValue)
            .SumAsync(e => e.Total!.Value, ct);
    }

    public Task<decimal> GetHouseholdSpentAsync(DateOnly month, CancellationToken ct = default)
    {
        var from = new DateTimeOffset(month.Year, month.Month, month.Day, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);

        return db.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date < to && e.Total.HasValue)
            .SumAsync(e => e.Total!.Value, ct);
    }

    public async Task<List<MemberContributionResponse>> GetMemberBreakdownAsync(DateOnly month, CancellationToken ct = default)
    {
        var from = new DateTimeOffset(month.Year, month.Month, month.Day, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);

        var grouped = await db.Expenses
            .AsNoTracking()
            .Where(e => e.Date >= from && e.Date < to && e.Total.HasValue)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(e => e.Total!.Value) })
            .ToListAsync(ct);

        var result = new List<MemberContributionResponse>(grouped.Count);
        foreach (var g in grouped)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == g.UserId, ct);
            result.Add(new MemberContributionResponse(g.UserId, user?.Username ?? g.UserId.ToString(), g.Total));
        }

        return result;
    }

    // ── Owner lookup ──────────────────────────────────────────────────────────

    public async Task<Guid?> FindOwnerUserIdAsync(CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Role == ExpenseTracker.Auth.Entities.UserRole.Admin, ct);
        return user?.Id;
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    public Task<NotificationEntity?> FindUndismissedAlertAsync(
        Guid userId, Guid budgetId, string type, DateOnly month, CancellationToken ct = default)
    {
        var from = new DateTimeOffset(month.Year, month.Month, month.Day, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);

        return db.Notifications.FirstOrDefaultAsync(n =>
            n.UserId == userId
            && n.BudgetId == budgetId
            && n.Type == type
            && n.CreatedAt >= from
            && n.CreatedAt < to
            && n.DismissedAt == null, ct);
    }

    public async Task AddNotificationAsync(NotificationEntity notification, CancellationToken ct = default)
        => await db.Notifications.AddAsync(notification, ct);

    public Task<NotificationEntity?> FindNotificationByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
        => db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

    public Task<List<NotificationEntity>> GetUnreadNotificationsAsync(Guid userId, CancellationToken ct = default)
        => db.Notifications
              .AsNoTracking()
              .Where(n => n.UserId == userId && n.DismissedAt == null)
              .OrderByDescending(n => n.CreatedAt)
              .ToListAsync(ct);

    // ── Budget History ────────────────────────────────────────────────────────

    public Task<bool> HasHistoryForMonthAsync(Guid budgetId, DateOnly month, CancellationToken ct = default)
        => db.BudgetHistories.AnyAsync(h => h.BudgetId == budgetId && h.Month == month, ct);

    public async Task AddHistoryAsync(BudgetHistoryEntity history, CancellationToken ct = default)
        => await db.BudgetHistories.AddAsync(history, ct);

    public async Task<List<BudgetHistoryEntity>> GetHistoryByUserAndMonthAsync(
        Guid userId, DateOnly month, CancellationToken ct = default)
    {
        // Join budget_history with budgets where budgets.UserId == userId and month matches.
        var budgetIds = await db.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Select(b => b.Id)
            .ToListAsync(ct);

        return await db.BudgetHistories
            .AsNoTracking()
            .Where(h => budgetIds.Contains(h.BudgetId) && h.Month == month)
            .ToListAsync(ct);
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
