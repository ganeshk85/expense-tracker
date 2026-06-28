using ExpenseTracker.Budget.Repositories;
using Microsoft.EntityFrameworkCore;
using BudgetEntity = ExpenseTracker.Budget.Entities.Budget;

namespace ExpenseTracker.Api.Data.Repositories;

public sealed class BudgetRepository(AppDbContext db) : IBudgetRepository
{
    public Task<List<BudgetEntity>> ListByUserAsync(Guid userId, CancellationToken ct = default)
        => db.Budgets.AsNoTracking().Where(b => b.UserId == userId).OrderBy(b => b.Category).ToListAsync(ct);

    public Task<BudgetEntity?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Budgets.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<BudgetEntity?> FindByUserAndCategoryAsync(Guid userId, string category, CancellationToken ct = default)
        => db.Budgets.FirstOrDefaultAsync(b => b.UserId == userId && b.Category == category, ct);

    public async Task AddAsync(BudgetEntity budget, CancellationToken ct = default)
        => await db.Budgets.AddAsync(budget, ct);

    public Task RemoveAsync(BudgetEntity budget, CancellationToken ct = default)
    {
        db.Budgets.Remove(budget);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
