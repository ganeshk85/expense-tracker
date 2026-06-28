using ExpenseTracker.Budget.Models;

namespace ExpenseTracker.Budget.Services;

public interface IBudgetService
{
    Task<BudgetResponse> CreateAsync(CreateBudgetRequest request, Guid userId, CancellationToken ct = default);
    Task<BudgetListResponse> ListAsync(Guid userId, CancellationToken ct = default);
    Task<BudgetResponse> UpdateAsync(Guid id, UpdateBudgetRequest request, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
