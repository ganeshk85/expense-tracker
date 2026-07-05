namespace ExpenseTracker.Expense.Models;

// ── Item models ─────────────────────────────────────────────────────────────

public sealed record CreateExpenseItemRequest(string Name, decimal Quantity, decimal UnitPrice);

public sealed record ExpenseItemsListResponse(IReadOnlyList<ExpenseItemResponse> Items);

// ── Share models ─────────────────────────────────────────────────────────────

public sealed record ExpenseShareEntryRequest(Guid UserId, decimal? Amount, decimal? Percentage);

public sealed record AssignSharesRequest(IReadOnlyList<ExpenseShareEntryRequest> Shares);

public sealed record ExpenseShareResponse(Guid Id, Guid UserId, decimal? Amount, decimal? Percentage);

// ── Receipt summary ──────────────────────────────────────────────────────────

public sealed record ReceiptSummaryResponse(Guid Id, string? ThumbnailUrl, string Status);

// ── Expense CRUD ─────────────────────────────────────────────────────────────

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
    string? Barcode,
    string? BarcodeType,
    IReadOnlyList<ExpenseItemResponse> Items,
    bool IsShared,
    IReadOnlyList<ExpenseShareResponse> Shares,
    IReadOnlyList<ReceiptSummaryResponse> Receipts,
    IReadOnlyList<ExpenseAttachmentResponse> Attachments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    // Set by Intelligence layer on create/get; null when no pending duplicate.
    public DuplicateWarning? DuplicateWarning { get; init; }
    // Set by Intelligence layer on OCR correction; null for manual expenses.
    public string? SuggestedCategory { get; init; }
    public string? SuggestionConfidence { get; init; }
}

// ── Attachment models ────────────────────────────────────────────────────────

public sealed record ExpenseAttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DownloadUrl,
    DateTimeOffset CreatedAt);

public sealed record AttachmentListResponse(IReadOnlyList<ExpenseAttachmentResponse> Attachments);

public sealed record ExpenseListResponse(
    IReadOnlyList<ExpenseResponse> Items,
    int Total,
    int Page,
    int PageSize);

// ── Search ───────────────────────────────────────────────────────────────────

/// <summary>All parameters are optional; only supplied parameters filter the result set.</summary>
public sealed record SearchExpensesRequest(
    string? Q,
    string? Category,
    string? Merchant,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    decimal? MinAmount,
    decimal? MaxAmount,
    string[]? Tags,
    int Page = 1,
    int PageSize = 50);

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
