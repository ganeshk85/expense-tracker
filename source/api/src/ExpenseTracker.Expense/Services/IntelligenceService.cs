using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Shared;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Expense.Services;

public sealed class IntelligenceService(
    IIntelligenceRepository repo,
    ILogger<IntelligenceService> logger) : IIntelligenceService
{
    private const int TagSuggestionLimit = 5;
    private const int MinSampleSize = 5;

    public async Task<string?> GetSuggestedCategoryAsync(
        Guid householdId, string? merchantName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
            return null;

        var normalized = MerchantNormalizer.Normalize(merchantName);
        if (string.IsNullOrEmpty(normalized))
            return null;

        var entry = await repo.FindMerchantCategoryAsync(householdId, normalized, ct);
        return entry?.Category;
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

        // Suppress if already dismissed.
        if (await repo.IsDismissedAsync(match.Value.ExpenseId, ct))
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
        var tags = await repo.GetTagSuggestionsAsync(householdId, normalized, TagSuggestionLimit, ct);
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
}
