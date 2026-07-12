using ExpenseTracker.Expense.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Expense.Services;

/// <summary>
/// Nightly job that scans the household's confirmed expense history for recurring
/// merchant+amount patterns (US-INT-06). Runs once per day; checks hourly so a missed
/// run (e.g. app restart) is picked up the same day rather than skipped entirely.
/// </summary>
public sealed class RecurringExpenseDetectionService(
    IServiceScopeFactory scopeFactory,
    ILogger<RecurringExpenseDetectionService> logger) : BackgroundService
{
    private const int CheckIntervalHours = 1;
    private DateOnly? _lastRunDate;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (_lastRunDate != today)
                {
                    await RunDetectionAsync(stoppingToken);
                    _lastRunDate = today;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RecurringExpenseDetectionService encountered an unexpected error.");
            }

            await Task.Delay(TimeSpan.FromHours(CheckIntervalHours), stoppingToken);
        }
    }

    private async Task RunDetectionAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IIntelligenceRepository>();

        // Single-household deployment — GetHouseholdIdForUserAsync ignores the user id.
        var householdId = await repo.GetHouseholdIdForUserAsync(Guid.Empty, ct);
        await repo.DetectRecurringExpensesAsync(householdId, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Recurring expense detection completed for household {HouseholdId}.", householdId);
    }
}
