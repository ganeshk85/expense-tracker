namespace ExpenseTracker.Expense.Models;

// ── Merchant-category map ─────────────────────────────────────────────────────

public sealed record MerchantCategoryMapEntry(
    string MerchantNameNormalized,
    string Category,
    int ConfirmedCount,
    DateTimeOffset LastConfirmedAt);

public sealed record MerchantCategoryMapResponse(IReadOnlyList<MerchantCategoryMapEntry> Items);

public sealed record CategorySuggestion(string Category, string Confidence); // Confidence: "high" | "low"

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

// ── Merchant field templates (US-INT-05) ──────────────────────────────────────

public sealed record MerchantFieldTemplateEntry(
    string MerchantNameNormalized,
    string FieldName,
    double RegionX,
    double RegionY,
    double RegionW,
    double RegionH,
    int SampleCount,
    DateTimeOffset LastUpdated);

public sealed record MerchantFieldTemplatesResponse(IReadOnlyList<MerchantFieldTemplateEntry> Items);

public sealed record UpsertMerchantTemplateRequest(
    string MerchantName,
    string FieldName,
    double RegionX,
    double RegionY,
    double RegionW,
    double RegionH);

// ── Recurring expenses (US-INT-06) ────────────────────────────────────────────

public sealed record RecurringExpenseEntry(
    Guid Id,
    string MerchantNameNormalized,
    decimal AverageAmount,
    int TypicalDayOfMonth,
    string Confidence, // "confirmed" | "likely"
    DateTimeOffset LastDetectedAt,
    DateTimeOffset? SnoozedUntil);

public sealed record RecurringExpensesResponse(IReadOnlyList<RecurringExpenseEntry> Items);

// ── Merchant aliases (US-INT-07) ──────────────────────────────────────────────

public sealed record MerchantAliasEntry(
    Guid Id,
    string AliasNormalized,
    string CanonicalNormalized,
    DateTimeOffset CreatedAt);

public sealed record MerchantAliasesResponse(IReadOnlyList<MerchantAliasEntry> Items);

public sealed record CreateMerchantAliasRequest(string Alias, string Canonical);

// ── Intelligence settings summary (US-INT-08) ─────────────────────────────────

public sealed record IntelligenceSummaryResponse(
    int MerchantMappings,
    int FieldTemplates,
    int RecurringExpenses,
    int Aliases);
