using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Shared;
using ExpenseTracker.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Expense.Services;

public sealed class IntelligenceService(
    IIntelligenceRepository repo,
    ILogger<IntelligenceService> logger) : IIntelligenceService
{
    private const int TagSuggestionLimit = 5;
    private const int MinSampleSize = 5;
    private const int HighConfidenceConfirmedCount = 3;

    public async Task<CategorySuggestion?> GetSuggestedCategoryAsync(
        Guid householdId, string? merchantName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            return null;

        var normalized = MerchantNormalizer.Normalize(merchantName);
        if (string.IsNullOrEmpty(normalized))
            return null;

        var canonical = await repo.ResolveAliasAsync(householdId, normalized, ct);
        var entry = await repo.FindMerchantCategoryAsync(householdId, canonical, ct);
        if (entry is null)
            return null;

        var confidence = entry.ConfirmedCount >= HighConfidenceConfirmedCount ? "high" : "low";
        return new CategorySuggestion(entry.Category, confidence);
    }

    public async Task<DuplicateWarning?> CheckDuplicateAsync(
        Guid householdId, string? merchantName, decimal? amount,
        DateTimeOffset? expenseDate, Guid excludeExpenseId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(merchantName) || amount is null or <= 0 || expenseDate is null)
            return null;

        var normalized = MerchantNormalizer.Normalize(merchantName);
        if (string.IsNullOrEmpty(normalized))
            return null;

        var match = await repo.FindPotentialDuplicateAsync(
            householdId, normalized, amount.Value, expenseDate.Value, ct);

        if (match is null || match.Value.ExpenseId == excludeExpenseId)
            return null;

        // Suppress if the user already dismissed this warning on the current expense.
        // Dismissal is recorded against excludeExpenseId (the expense showing the banner),
        // not match.Value.ExpenseId (the older expense it matches) — those are different rows.
        if (await repo.IsDismissedAsync(excludeExpenseId, ct))
            return null;

        var dateDiff = Math.Abs((expenseDate.Value.Date - (match.Value.Date?.Date ?? expenseDate.Value.Date)).Days);
        var confidence = dateDiff == 0 ? "high" : "possible";

        logger.LogInformation(
            "Duplicate warning: expense for merchant={Merchant} amount={Amount} matches existing {ExistingId} confidence={Confidence}",
            normalized, amount, match.Value.ExpenseId, confidence);

        return new DuplicateWarning(match.Value.ExpenseId, match.Value.Date, confidence);
    }

    public async Task DismissDuplicateAsync(
        Guid expenseId, Guid userId, CancellationToken ct = default)
    {
        await repo.DismissDuplicateAsync(expenseId, userId, ct);
        await repo.SaveChangesAsync(ct);
        logger.LogInformation("Duplicate dismissed for expense {ExpenseId} by user {UserId}", expenseId, userId);
    }

    public async Task<TagSuggestionsResponse> GetTagSuggestionsAsync(
        Guid householdId, string? merchantName, CancellationToken ct = default)
    {
        var normalized = MerchantNormalizer.Normalize(merchantName);
        var canonical = string.IsNullOrEmpty(normalized)
            ? normalized
            : await repo.ResolveAliasAsync(householdId, normalized, ct);
        var tags = await repo.GetTagSuggestionsAsync(householdId, canonical, TagSuggestionLimit, ct);
        return new TagSuggestionsResponse(tags);
    }

    public async Task<MerchantCategoryMapResponse> GetMerchantCategoryMapAsync(
        Guid householdId, CancellationToken ct = default)
    {
        var entries = await repo.GetMerchantCategoryMapAsync(householdId, ct);
        var items = entries
            .Select(e => new MerchantCategoryMapEntry(
                e.MerchantNameNormalized,
                e.Category,
                e.ConfirmedCount,
                e.LastConfirmedAt))
            .ToList()
            .AsReadOnly();

        return new MerchantCategoryMapResponse(items);
    }

    public async Task<OcrAccuracyResponse> GetOcrAccuracyAsync(CancellationToken ct = default)
    {
        var rows = await repo.GetOcrAccuracyAsync(ct);
        var items = rows.Select(r =>
        {
            var insufficient = r.TotalExtractions < MinSampleSize;
            double? rate = insufficient ? null : 1.0 - ((double)r.TotalCorrections / r.TotalExtractions);
            return new OcrFieldAccuracyEntry(
                r.MerchantNameNormalized,
                r.FieldName,
                rate,
                r.TotalExtractions,
                insufficient);
        }).ToList().AsReadOnly();

        return new OcrAccuracyResponse(items);
    }

    // ── Merchant field templates (US-INT-05) ──────────────────────────────────

    public async Task UpsertMerchantTemplateAsync(
        Guid householdId, UpsertMerchantTemplateRequest request, CancellationToken ct = default)
    {
        var normalized = MerchantNormalizer.Normalize(request.MerchantName);
        if (string.IsNullOrEmpty(normalized))
            return;

        var canonical = await repo.ResolveAliasAsync(householdId, normalized, ct);
        await repo.UpsertMerchantTemplateAsync(
            householdId, canonical, request.FieldName,
            request.RegionX, request.RegionY, request.RegionW, request.RegionH, ct);
        await repo.SaveChangesAsync(ct);
    }

    public async Task<MerchantFieldTemplatesResponse> GetMerchantTemplatesAsync(
        Guid householdId, CancellationToken ct = default)
    {
        var rows = await repo.GetMerchantTemplatesAsync(householdId, ct);
        var items = rows.Select(t => new MerchantFieldTemplateEntry(
            t.MerchantNameNormalized, t.FieldName, t.RegionX, t.RegionY, t.RegionW, t.RegionH,
            t.SampleCount, t.LastUpdated)).ToList().AsReadOnly();

        return new MerchantFieldTemplatesResponse(items);
    }

    public async Task<MerchantFieldTemplatesResponse> GetMerchantTemplatesForMerchantAsync(
        Guid householdId, string merchantName, CancellationToken ct = default)
    {
        var normalized = MerchantNormalizer.Normalize(merchantName);
        var canonical = string.IsNullOrEmpty(normalized)
            ? normalized
            : await repo.ResolveAliasAsync(householdId, normalized, ct);

        var rows = await repo.GetMerchantTemplatesAsync(householdId, ct);
        var items = rows
            .Where(t => t.MerchantNameNormalized == canonical)
            .Select(t => new MerchantFieldTemplateEntry(
                t.MerchantNameNormalized, t.FieldName, t.RegionX, t.RegionY, t.RegionW, t.RegionH,
                t.SampleCount, t.LastUpdated))
            .ToList().AsReadOnly();

        return new MerchantFieldTemplatesResponse(items);
    }

    public async Task<int> DeleteMerchantTemplatesAsync(
        Guid householdId, string merchantName, CancellationToken ct = default)
    {
        var normalized = MerchantNormalizer.Normalize(merchantName);
        var deleted = await repo.DeleteMerchantTemplatesAsync(householdId, normalized, ct);
        await repo.SaveChangesAsync(ct);

        if (deleted > 0)
            logger.LogInformation("Field templates deleted for merchant={Merchant} (household={HouseholdId})", normalized, householdId);

        return deleted;
    }

    // ── Recurring expenses (US-INT-06) ─────────────────────────────────────────

    public async Task<RecurringExpensesResponse> GetRecurringExpensesAsync(
        Guid householdId, CancellationToken ct = default)
    {
        var rows = await repo.GetRecurringExpensesAsync(householdId, ct);
        var items = rows.Select(r => new RecurringExpenseEntry(
            r.Id, r.MerchantNameNormalized, r.AverageAmount, r.TypicalDayOfMonth,
            r.Confidence, r.LastDetectedAt, r.SnoozedUntil)).ToList().AsReadOnly();

        return new RecurringExpensesResponse(items);
    }

    public async Task SnoozeRecurringExpenseAsync(
        Guid householdId, Guid id, int days, CancellationToken ct = default)
    {
        await repo.SnoozeRecurringExpenseAsync(householdId, id, days, ct);
        await repo.SaveChangesAsync(ct);
    }

    public async Task DetectRecurringExpensesAsync(Guid householdId, CancellationToken ct = default)
    {
        await repo.DetectRecurringExpensesAsync(householdId, ct);
        await repo.SaveChangesAsync(ct);
    }

    // ── Merchant aliases (US-INT-07) ───────────────────────────────────────────

    public async Task<MerchantAliasEntry> CreateAliasAsync(
        Guid householdId, CreateMerchantAliasRequest request, Guid createdBy, CancellationToken ct = default)
    {
        var alias = MerchantNormalizer.Normalize(request.Alias);
        var canonical = MerchantNormalizer.Normalize(request.Canonical);

        if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(canonical))
            throw new ValidationException("Alias and canonical name are both required.");
        if (alias == canonical)
            throw new ValidationException("Alias and canonical name must not be the same.");

        var entity = await repo.CreateAliasAsync(householdId, alias, canonical, createdBy, ct);
        await repo.SaveChangesAsync(ct);

        return new MerchantAliasEntry(entity.Id, entity.AliasNormalized, entity.CanonicalNormalized, entity.CreatedAt);
    }

    public async Task<MerchantAliasesResponse> GetAliasesAsync(Guid householdId, CancellationToken ct = default)
    {
        var rows = await repo.GetAliasesAsync(householdId, ct);
        var items = rows.Select(a => new MerchantAliasEntry(a.Id, a.AliasNormalized, a.CanonicalNormalized, a.CreatedAt))
            .ToList().AsReadOnly();

        return new MerchantAliasesResponse(items);
    }

    public async Task DeleteAliasAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        await repo.DeleteAliasAsync(householdId, id, ct);
        await repo.SaveChangesAsync(ct);
    }

    // ── Intelligence summary (US-INT-08) ───────────────────────────────────────

    public async Task<IntelligenceSummaryResponse> GetSummaryAsync(Guid householdId, CancellationToken ct = default)
    {
        var counts = await repo.GetSummaryCountsAsync(householdId, ct);
        return new IntelligenceSummaryResponse(
            counts.MerchantMappings, counts.FieldTemplates, counts.RecurringExpenses, counts.Aliases);
    }
}
