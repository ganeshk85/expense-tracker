using ExpenseTracker.Budget.Entities;
using ExpenseTracker.Budget.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Budget.Services;

public sealed class BudgetResetService(
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetResetService> logger) : BackgroundService
{
    private const int CheckIntervalHours = 1;
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now.Day == 1)
                    await TrySnapshotWithRetriesAsync(now, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "BudgetResetService encountered an unexpected error.");
            }

            await Task.Delay(TimeSpan.FromHours(CheckIntervalHours), stoppingToken);
        }
    }

    private async Task TrySnapshotWithRetriesAsync(DateTime now, CancellationToken ct)
    {
        // Prior month = the month that just ended.
        var priorMonth = new DateOnly(now.Year, now.Month, 1).AddMonths(-1);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await SnapshotAsync(priorMonth, ct);
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                logger.LogWarning(ex, "Budget reset snapshot attempt {Attempt} failed. Retrying in 5 minutes.", attempt);
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Budget reset snapshot failed after {MaxRetries} attempts for month {Month}.", MaxRetries, priorMonth);
                await NotifyOwnerOfFailureAsync(priorMonth, ct);
            }
        }
    }

    private async Task SnapshotAsync(DateOnly month, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBudgetRepository>();

        var budgets = await repo.ListAllAsync(ct);
        var added = 0;

        foreach (var budget in budgets)
        {
            if (await repo.HasHistoryForMonthAsync(budget.Id, month, ct))
                continue;

            decimal spent = budget.Type == BudgetType.Household
                ? await repo.GetHouseholdSpentAsync(month, ct)
                : await repo.GetCategorySpentAsync(budget.UserId, budget.Category, month, ct);

            var history = new BudgetHistory
            {
                BudgetId = budget.Id,
                Month = month,
                Limit = budget.MonthlyLimit,
                Spent = spent,
            };

            await repo.AddHistoryAsync(history, ct);
            added++;
        }

        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Budget reset snapshot complete for {Month}: {Count} budgets snapshotted.", month, added);
    }

    private async Task NotifyOwnerOfFailureAsync(DateOnly month, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBudgetRepository>();

            var ownerUserId = await repo.FindOwnerUserIdAsync(ct);
            if (ownerUserId is null) return;

            var note = new Notification
            {
                UserId = ownerUserId.Value,
                Type = NotificationType.BudgetDeleted,
                Message = $"Monthly budget reset failed for {month:yyyy-MM}. Please check system logs.",
                BudgetId = null,
            };

            await repo.AddNotificationAsync(note, ct);
            await repo.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify Owner of budget reset failure.");
        }
    }
}
