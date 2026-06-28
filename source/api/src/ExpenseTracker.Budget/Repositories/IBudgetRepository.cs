using BudgetEntity = ExpenseTracker.Budget.Entities.Budget;

namespace ExpenseTracker.Budget.Repositories;

public interface IBudgetRepository
{
    Task<List<BudgetEntity>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<BudgetEntity?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<BudgetEntity?> FindByUserAndCategoryAsync(Guid userId, string category, CancellationToken ct = default);
    Task AddAsync(BudgetEntity budget, CancellationToken ct = default);
    Task RemoveAsync(BudgetEntity budget, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
