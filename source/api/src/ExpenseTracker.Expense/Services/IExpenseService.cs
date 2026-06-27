using ExpenseTracker.Expense.Models;

namespace ExpenseTracker.Expense.Services;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateManualAsync(
        CreateExpenseRequest request, Guid userId, CancellationToken ct = default);

    Task<ExpenseListResponse> ListAsync(
        Guid userId, string userRole, bool allHousehold,
        int page, int pageSize, CancellationToken ct = default);

    Task<ExpenseResponse> GetByIdAsync(
        Guid id, Guid userId, string userRole, CancellationToken ct = default);

    Task<ExpenseResponse> UpdateAsync(
        Guid id, UpdateExpenseRequest request,
        Guid userId, string userRole, CancellationToken ct = default);

    Task DeleteAsync(
        Guid id, Guid userId, string userRole, string ipAddress, CancellationToken ct = default);

    Task<ExpenseResponse> ApplyCorrectionsAsync(
        Guid id, CorrectExpenseRequest request,
        Guid userId, string userRole, string ipAddress, CancellationToken ct = default);
}
