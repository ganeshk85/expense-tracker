namespace ExpenseTracker.Shared;

/// <summary>
/// Decoupled trigger for budget alert recalculation after an expense write.
/// Defined in Shared so Expense module can reference it without depending on Budget module.
/// </summary>
public interface IBudgetAlertService
{
    Task CheckAndFireAlertsAsync(Guid userId, string? category, CancellationToken ct = default);
}
