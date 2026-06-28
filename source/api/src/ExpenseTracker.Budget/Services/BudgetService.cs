using ExpenseTracker.Budget.Entities;
using ExpenseTracker.Budget.Models;
using ExpenseTracker.Budget.Repositories;
using ExpenseTracker.Expense.Models;
using ExpenseTracker.Shared;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using BudgetEntity = ExpenseTracker.Budget.Entities.Budget;
using NotificationEntity = ExpenseTracker.Budget.Entities.Notification;

namespace ExpenseTracker.Budget.Services;

public sealed class BudgetService(
    IBudgetRepository repo,
    ILogger<BudgetService> logger) : IBudgetService, IBudgetAlertService
{
    private const string OwnerRole = "Admin";

    public async Task<BudgetResponse> CreateAsync(
        CreateBudgetRequest request, Guid userId, string userRole, CancellationToken ct = default)
    {
        var type = request.Type ?? BudgetType.Category;

        if (type != BudgetType.Category && type != BudgetType.Household)
            throw new ValidationException($"Invalid budget type '{type}'. Must be 'category' or 'household'.");

        if (type == BudgetType.Household && userRole != OwnerRole)
            throw new ForbiddenException("Only an Owner can create a household budget.");

        if (request.MonthlyLimit <= 0)
            throw new ValidationException("Monthly limit must be greater than zero.");

        if (type == BudgetType.Category)
        {
            if (!ExpenseCategory.IsValid(request.Category) || string.IsNullOrWhiteSpace(request.Category))
                throw new ValidationException($"Invalid category '{request.Category}'.");

            var existing = await repo.FindByUserAndCategoryAsync(userId, request.Category, ct);
            if (existing is not null)
                throw new ConflictException($"A budget for category '{request.Category}' already exists.");
        }
        else
        {
            var existingHousehold = await repo.FindHouseholdBudgetByUserAsync(userId, ct);
            if (existingHousehold is not null)
                throw new ConflictException("A household budget already exists.");
        }

        var budget = new BudgetEntity
        {
            UserId = userId,
            Category = type == BudgetType.Household ? "household" : request.Category,
            MonthlyLimit = request.MonthlyLimit,
            Type = type,
        };

        await repo.AddAsync(budget, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Budget {Id} ({Type}) created for user {UserId}", budget.Id, type, userId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await BuildResponseAsync(budget, today, userRole, ct);
    }

    public async Task<BudgetListResponse> ListAsync(Guid userId, string userRole, CancellationToken ct = default)
    {
        var budgets = await repo.ListByUserAsync(userId, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var month = new DateOnly(today.Year, today.Month, 1);

        var responses = new List<BudgetResponse>(budgets.Count);
        foreach (var b in budgets)
            responses.Add(await BuildResponseAsync(b, month, userRole, ct));

        return new BudgetListResponse(responses.AsReadOnly());
    }

    public async Task<BudgetResponse> UpdateAsync(
        Guid id, UpdateBudgetRequest request, Guid userId, CancellationToken ct = default)
    {
        if (request.MonthlyLimit <= 0)
            throw new ValidationException("Monthly limit must be greater than zero.");

        var budget = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Budget", id);

        if (budget.UserId != userId)
            throw new ForbiddenException("You do not have access to this budget.");

        budget.MonthlyLimit = request.MonthlyLimit;
        budget.UpdatedAt = DateTimeOffset.UtcNow;

        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Budget {Id} updated by user {UserId}", id, userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await BuildResponseAsync(budget, today, null, ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var budget = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Budget", id);

        if (budget.UserId != userId)
            throw new ForbiddenException("You do not have access to this budget.");

        var isHousehold = budget.Type == BudgetType.Household;

        await repo.RemoveAsync(budget, ct);

        if (isHousehold)
        {
            var ownerUserId = await repo.FindOwnerUserIdAsync(ct);
            if (ownerUserId.HasValue)
            {
                var note = new NotificationEntity
                {
                    UserId = ownerUserId.Value,
                    Type = NotificationType.BudgetDeleted,
                    Message = "Shared household budget was removed.",
                    BudgetId = null,
                };
                await repo.AddNotificationAsync(note, ct);
            }
        }

        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Budget {Id} deleted by user {UserId}", id, userId);
    }

    public async Task<BudgetHistoryListResponse> GetHistoryAsync(
        Guid userId, string month, CancellationToken ct = default)
    {
        if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var parsedMonth))
            throw new ValidationException("Month must be in YYYY-MM format.");

        var items = await repo.GetHistoryByUserAndMonthAsync(userId, parsedMonth, ct);
        var responses = items.Select(h => new BudgetHistoryResponse(
            h.Id,
            h.BudgetId,
            h.Month.ToString("yyyy-MM"),
            h.Limit,
            h.Spent)).ToList().AsReadOnly();

        return new BudgetHistoryListResponse(responses);
    }

    public async Task CheckAndFireAlertsAsync(Guid userId, string? category, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(category)) return;

        var budget = await repo.FindByUserAndCategoryAsync(userId, category, ct);
        if (budget is null) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var month = new DateOnly(today.Year, today.Month, 1);
        var spent = await repo.GetCategorySpentAsync(userId, category, month, ct);

        if (budget.MonthlyLimit <= 0) return;
        var progress = spent / budget.MonthlyLimit * 100m;

        if (progress >= 100m)
        {
            var existingExceeded = await repo.FindUndismissedAlertAsync(userId, budget.Id, NotificationType.BudgetExceeded, month, ct);
            if (existingExceeded is null)
            {
                var note = new NotificationEntity
                {
                    UserId = userId,
                    Type = NotificationType.BudgetExceeded,
                    Message = $"Budget for '{category}' has been exceeded ({progress:F0}% of limit).",
                    BudgetId = budget.Id,
                };
                await repo.AddNotificationAsync(note, ct);
                await repo.SaveChangesAsync(ct);
                logger.LogInformation("Budget exceeded alert fired for user {UserId} category {Category}", userId, category);
            }
        }
        else if (progress >= 80m)
        {
            var existingThreshold = await repo.FindUndismissedAlertAsync(userId, budget.Id, NotificationType.BudgetThreshold, month, ct);
            if (existingThreshold is null)
            {
                var note = new NotificationEntity
                {
                    UserId = userId,
                    Type = NotificationType.BudgetThreshold,
                    Message = $"Budget for '{category}' is at {progress:F0}% of the monthly limit.",
                    BudgetId = budget.Id,
                };
                await repo.AddNotificationAsync(note, ct);
                await repo.SaveChangesAsync(ct);
                logger.LogInformation("Budget threshold alert fired for user {UserId} category {Category}", userId, category);
            }
        }
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await repo.GetUnreadNotificationsAsync(userId, ct);
        var responses = items.Select(n => new NotificationResponse(
            n.Id, n.Type, n.Message, n.BudgetId, n.CreatedAt, n.DismissedAt))
            .ToList().AsReadOnly();
        return new NotificationListResponse(responses);
    }

    public async Task DismissNotificationAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var note = await repo.FindNotificationByIdAsync(id, userId, ct)
            ?? throw new NotFoundException("Notification", id);

        if (note.DismissedAt.HasValue)
            throw new ValidationException("Notification is already dismissed.");

        note.DismissedAt = DateTimeOffset.UtcNow;
        await repo.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<BudgetResponse> BuildResponseAsync(
        BudgetEntity budget, DateOnly month, string? userRole, CancellationToken ct)
    {
        decimal spent;
        IReadOnlyList<MemberContributionResponse>? breakdown = null;

        if (budget.Type == BudgetType.Household)
        {
            spent = await repo.GetHouseholdSpentAsync(month, ct);
            if (userRole == OwnerRole)
                breakdown = (await repo.GetMemberBreakdownAsync(month, ct)).AsReadOnly();
        }
        else
        {
            spent = await repo.GetCategorySpentAsync(budget.UserId, budget.Category, month, ct);
        }

        var progress = budget.MonthlyLimit > 0
            ? Math.Round(spent / budget.MonthlyLimit * 100m, 2)
            : 0m;

        return new BudgetResponse(
            budget.Id,
            budget.Category,
            budget.MonthlyLimit,
            budget.Type,
            spent,
            progress,
            breakdown,
            budget.CreatedAt,
            budget.UpdatedAt);
    }
}
