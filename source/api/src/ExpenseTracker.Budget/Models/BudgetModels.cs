namespace ExpenseTracker.Budget.Models;

public sealed record CreateBudgetRequest(string Category, decimal MonthlyLimit);

public sealed record UpdateBudgetRequest(decimal MonthlyLimit);

public sealed record BudgetResponse(
    Guid Id,
    string Category,
    decimal MonthlyLimit,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record BudgetListResponse(IReadOnlyList<BudgetResponse> Items);
