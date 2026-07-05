namespace ExpenseTracker.Expense.Models;

// ── Merchant-category map ─────────────────────────────────────────────────────

public sealed record MerchantCategoryMapEntry(
    string MerchantNameNormalized,
    string Category,
    int ConfirmedCount,
    DateTimeOffset LastConfirmedAt);

public sealed record MerchantCategoryMapResponse(IReadOnlyList<MerchantCategoryMapEntry> Items);

// ── Duplicate warning ─────────────────────────────────────────────────────────

public sealed record DuplicateWarning(
    Guid ExistingExpenseId,
    DateTimeOffset? ExistingDate,
    string Confidence); // "high" | "possible"

// ── Tag suggestions ───────────────────────────────────────────────────────────

public sealed record TagSuggestionsResponse(IReadOnlyList<string> Tags);

// ── OCR accuracy ──────────────────────────────────────────────────────────────

public sealed record OcrFieldAccuracyEntry(
    string Merchant,
    string Field,
    double? AccuracyRate,
    int SampleSize,
    bool InsufficientData);

public sealed record OcrAccuracyResponse(IReadOnlyList<OcrFieldAccuracyEntry> Items);
