namespace ExpenseTracker.Budget.Models;

public sealed record CreateBudgetRequest(
    string Category,
    decimal MonthlyLimit,
    string? Type = null);

public sealed record UpdateBudgetRequest(decimal MonthlyLimit);

public sealed record MemberContributionResponse(
    Guid UserId,
    string DisplayName,
    decimal Contributed);

public sealed record BudgetResponse(
    Guid Id,
    string Category,
    decimal MonthlyLimit,
    string Type,
    decimal Spent,
    decimal ProgressPercent,
    IReadOnlyList<MemberContributionResponse>? MemberBreakdown,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record BudgetListResponse(IReadOnlyList<BudgetResponse> Items);

public sealed record BudgetHistoryResponse(
    Guid Id,
    Guid BudgetId,
    string Month,
    decimal Limit,
    decimal Spent);

public sealed record BudgetHistoryListResponse(IReadOnlyList<BudgetHistoryResponse> Items);

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Message,
    Guid? BudgetId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DismissedAt);

public sealed record NotificationListResponse(IReadOnlyList<NotificationResponse> Notifications);
