using ExpenseTracker.Budget.Models;
using ExpenseTracker.Budget.Repositories;
using ExpenseTracker.Expense.Models;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using BudgetEntity = ExpenseTracker.Budget.Entities.Budget;

namespace ExpenseTracker.Budget.Services;

public sealed class BudgetService(
    IBudgetRepository repo,
    ILogger<BudgetService> logger) : IBudgetService
{
    public async Task<BudgetResponse> CreateAsync(
        CreateBudgetRequest request, Guid userId, CancellationToken ct = default)
    {
        if (!ExpenseCategory.IsValid(request.Category) || string.IsNullOrWhiteSpace(request.Category))
            throw new ValidationException($"Invalid category '{request.Category}'.");

        if (request.MonthlyLimit <= 0)
            throw new ValidationException("Monthly limit must be greater than zero.");

        var existing = await repo.FindByUserAndCategoryAsync(userId, request.Category, ct);
        if (existing is not null)
            throw new ConflictException($"A budget for category '{request.Category}' already exists.");

        var budget = new BudgetEntity
        {
            UserId = userId,
            Category = request.Category,
            MonthlyLimit = request.MonthlyLimit,
        };

        await repo.AddAsync(budget, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Budget {Id} created for user {UserId} category {Category}", budget.Id, userId, request.Category);
        return ToResponse(budget);
    }

    public async Task<BudgetListResponse> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var budgets = await repo.ListByUserAsync(userId, ct);
        return new BudgetListResponse(budgets.Select(ToResponse).ToList().AsReadOnly());
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
        return ToResponse(budget);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var budget = await repo.FindByIdAsync(id, ct)
            ?? throw new NotFoundException("Budget", id);

        if (budget.UserId != userId)
            throw new ForbiddenException("You do not have access to this budget.");

        await repo.RemoveAsync(budget, ct);
        await repo.SaveChangesAsync(ct);

        logger.LogInformation("Budget {Id} deleted by user {UserId}", id, userId);
    }

    private static BudgetResponse ToResponse(BudgetEntity b) =>
        new(b.Id, b.Category, b.MonthlyLimit, b.CreatedAt, b.UpdatedAt);
}
