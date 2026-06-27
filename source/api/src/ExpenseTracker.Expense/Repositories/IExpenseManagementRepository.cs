using ExpenseEntity = ExpenseTracker.Ocr.Entities.Expense;

namespace ExpenseTracker.Expense.Repositories;

public interface IExpenseManagementRepository
{
    Task<ExpenseEntity?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <param name="userId">When null, returns expenses for all users (Owner view).</param>
    Task<(IReadOnlyList<ExpenseEntity> Items, int Total)> ListAsync(
        Guid? userId, int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(ExpenseEntity expense, CancellationToken ct = default);
    Task DeleteAsync(ExpenseEntity expense, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
