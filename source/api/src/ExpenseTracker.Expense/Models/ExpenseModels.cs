namespace ExpenseTracker.Expense.Models;

public sealed record CreateExpenseRequest(
    string? MerchantName,
    DateTimeOffset? Date,
    decimal Total,
    string? Category,
    string[]? Tags,
    string? Notes);

public sealed record UpdateExpenseRequest(
    string? MerchantName,
    string? MerchantAddress,
    DateTimeOffset? Date,
    string? Time,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal? Total,
    string? Category,
    string[]? Tags,
    string? Notes,
    IReadOnlyList<UpdateExpenseItemRequest>? Items);

public sealed record UpdateExpenseItemRequest(
    Guid? Id,
    string Name,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CorrectExpenseRequest(
    string? MerchantName,
    DateTimeOffset? Date,
    decimal? Total,
    decimal? Subtotal,
    decimal? TaxAmount,
    string? Category,
    string[]? Tags,
    string? Notes,
    IReadOnlyList<UpdateExpenseItemRequest>? Items);

public sealed record ExpenseItemResponse(
    Guid Id,
    string Name,
    decimal Quantity,
    decimal UnitPrice);

public sealed record ExpenseResponse(
    Guid Id,
    Guid? ReceiptId,
    Guid UserId,
    string? MerchantName,
    string? MerchantAddress,
    DateTimeOffset? Date,
    string? Time,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal? Total,
    string? Category,
    string[] Tags,
    string? Notes,
    string Source,
    string OcrStatus,
    string? ConfidenceJson,
    IReadOnlyList<ExpenseItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ExpenseListResponse(
    IReadOnlyList<ExpenseResponse> Items,
    int Total,
    int Page,
    int PageSize);

public static class ExpenseCategory
{
    public const string Groceries = "Groceries";
    public const string Dining = "Dining";
    public const string Utilities = "Utilities";
    public const string Transport = "Transport";
    public const string Health = "Health";
    public const string Other = "Other";

    private static readonly HashSet<string> AllValues = new(StringComparer.OrdinalIgnoreCase)
    {
        Groceries, Dining, Utilities, Transport, Health, Other
    };

    public static bool IsValid(string? category)
        => category is null || AllValues.Contains(category);
}
