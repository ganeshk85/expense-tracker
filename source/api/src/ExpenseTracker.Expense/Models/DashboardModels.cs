namespace ExpenseTracker.Expense.Models;

public sealed record CategoryBreakdownItem(string Category, decimal Amount, decimal Percentage);

public sealed record TopMerchantItem(string Merchant, decimal TotalSpent, int VisitCount);

public sealed record DashboardSummaryResponse(
    string Month,
    decimal TotalSpent,
    int ExpenseCount,
    IReadOnlyList<CategoryBreakdownItem> CategoryBreakdown,
    IReadOnlyList<TopMerchantItem> TopMerchants);
