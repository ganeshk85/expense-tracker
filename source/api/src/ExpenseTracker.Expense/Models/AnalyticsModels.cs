namespace ExpenseTracker.Expense.Models;

// ── Category Trend Report ─────────────────────────────────────────────────────

public sealed record CategoryMonthDataPoint(string Month, decimal Amount, bool IsSpiked);

public sealed record CategoryTrendSeries(string Category, IReadOnlyList<CategoryMonthDataPoint> Data);

public sealed record CategoryTrendResponse(
    IReadOnlyList<string> Months,
    IReadOnlyList<CategoryTrendSeries> Series);

// ── Merchant Analytics ────────────────────────────────────────────────────────

public sealed record MerchantRankItem(string Merchant, decimal TotalSpent, int VisitCount);

public sealed record MerchantRankingsResponse(IReadOnlyList<MerchantRankItem> Merchants);

public sealed record MerchantExpenseItem(
    string Id,
    string? Date,
    decimal? Total,
    string? Category,
    string? Notes);

public sealed record MerchantDetailResponse(
    string Merchant,
    decimal TotalSpent,
    int VisitCount,
    IReadOnlyList<MerchantExpenseItem> Expenses);
