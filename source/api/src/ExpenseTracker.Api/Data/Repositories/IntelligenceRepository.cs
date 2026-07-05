using ExpenseTracker.Expense.Entities;
using ExpenseTracker.Expense.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data.Repositories;

public sealed class IntelligenceRepository(AppDbContext db) : IIntelligenceRepository
{
    // Single-household deployment: all users share one implicit household.
    private static readonly Guid SystemHouseholdId = new("00000000-0000-0000-0000-000000000001");

    // ── Household resolution ──────────────────────────────────────────────────

    public Task<Guid> GetHouseholdIdForUserAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(SystemHouseholdId);

    // ── Merchant-category map ─────────────────────────────────────────────────

    public async Task<MerchantCategoryMap?> FindMerchantCategoryAsync(
        Guid householdId, string merchantNormalized, CancellationToken ct = default)
    {
        return await db.MerchantCategoryMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.HouseholdId == householdId &&
                m.MerchantNameNormalized == merchantNormalized, ct);
    }

    public async Task UpsertMerchantCategoryAsync(
        Guid householdId, string merchantNormalized, string category, CancellationToken ct = default)
    {
        var existing = await db.MerchantCategoryMaps
            .FirstOrDefaultAsync(m =>
                m.HouseholdId == householdId &&
                m.MerchantNameNormalized == merchantNormalized, ct);

        if (existing is null)
        {
            db.MerchantCategoryMaps.Add(new MerchantCategoryMap
            {
                HouseholdId = householdId,
                MerchantNameNormalized = merchantNormalized,
                Category = category,
                ConfirmedCount = 1,
                LastConfirmedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            // If the category has changed, reset the count on the new category.
            if (!string.Equals(existing.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                existing.Category = category;
                existing.ConfirmedCount = 1;
            }
            else
            {
                existing.ConfirmedCount++;
            }
            existing.LastConfirmedAt = DateTimeOffset.UtcNow;
        }
    }

    public async Task<IReadOnlyList<MerchantCategoryMap>> GetMerchantCategoryMapAsync(
        Guid householdId, CancellationToken ct = default)
    {
        return await db.MerchantCategoryMaps
            .AsNoTracking()
            .Where(m => m.HouseholdId == householdId)
            .OrderByDescending(m => m.ConfirmedCount)
            .ToListAsync(ct);
    }

    // ── Duplicate detection ───────────────────────────────────────────────────

    public async Task<(Guid ExpenseId, DateTimeOffset? Date)?> FindPotentialDuplicateAsync(
        Guid householdId, string merchantNormalized, decimal amount,
        DateTimeOffset expenseDate, CancellationToken ct = default)
    {
        var dateFrom = expenseDate.Date.AddDays(-1);
        var dateTo = expenseDate.Date.AddDays(1);

        // Compare lowercased merchant name against the normalized key.
        // Minor punctuation differences are tolerated at this stage.
        var match = await db.Expenses
            .AsNoTracking()
            .Where(e =>
                e.Total == amount &&
                e.Date.HasValue &&
                e.Date.Value.Date >= dateFrom &&
                e.Date.Value.Date <= dateTo &&
                e.MerchantName != null &&
                e.MerchantName.ToLower() == merchantNormalized)
            .Select(e => new { e.Id, e.Date })
            .FirstOrDefaultAsync(ct);

        return match is null ? null : (match.Id, match.Date);
    }

    public async Task<bool> IsDismissedAsync(Guid expenseId, CancellationToken ct = default)
    {
        return await db.DuplicateDismissals
            .AsNoTracking()
            .AnyAsync(d => d.ExpenseId == expenseId, ct);
    }

    public async Task DismissDuplicateAsync(
        Guid expenseId, Guid dismissedBy, CancellationToken ct = default)
    {
        var alreadyDismissed = await db.DuplicateDismissals
            .AnyAsync(d => d.ExpenseId == expenseId, ct);

        if (!alreadyDismissed)
        {
            db.DuplicateDismissals.Add(new DuplicateDismissal
            {
                ExpenseId = expenseId,
                DismissedBy = dismissedBy,
            });
        }
    }

    // ── Tag history ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> GetTagSuggestionsAsync(
        Guid householdId, string merchantNormalized, int maxResults, CancellationToken ct = default)
    {
        IReadOnlyList<string> tags;

        if (!string.IsNullOrEmpty(merchantNormalized))
        {
            tags = await db.MerchantTagHistories
                .AsNoTracking()
                .Where(t => t.HouseholdId == householdId &&
                            t.MerchantNameNormalized == merchantNormalized)
                .OrderByDescending(t => t.UseCount)
                .Select(t => t.Tag)
                .Take(maxResults)
                .ToListAsync(ct);

            if (tags.Count >= maxResults)
                return tags;
        }
        else
        {
            tags = [];
        }

        // Fallback: household-wide top tags to fill remaining slots.
        var needed = maxResults - tags.Count;
        var existing = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);

        var fallback = await db.MerchantTagHistories
            .AsNoTracking()
            .Where(t => t.HouseholdId == householdId)
            .GroupBy(t => t.Tag)
            .OrderByDescending(g => g.Sum(t => t.UseCount))
            .Select(g => g.Key)
            .Take(maxResults)
            .ToListAsync(ct);

        var combined = new List<string>(tags);
        foreach (var tag in fallback)
        {
            if (combined.Count >= maxResults) break;
            if (!existing.Contains(tag))
                combined.Add(tag);
        }

        return combined.AsReadOnly();
    }

    public async Task UpsertTagHistoryAsync(
        Guid householdId, string merchantNormalized, string[] tags, CancellationToken ct = default)
    {
        foreach (var tag in tags.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)))
        {
            var existing = await db.MerchantTagHistories
                .FirstOrDefaultAsync(t =>
                    t.HouseholdId == householdId &&
                    t.MerchantNameNormalized == merchantNormalized &&
                    t.Tag == tag, ct);

            if (existing is null)
            {
                db.MerchantTagHistories.Add(new MerchantTagHistory
                {
                    HouseholdId = householdId,
                    MerchantNameNormalized = merchantNormalized,
                    Tag = tag,
                    UseCount = 1,
                });
            }
            else
            {
                existing.UseCount++;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    // ── OCR accuracy ──────────────────────────────────────────────────────────

    public async Task UpsertOcrFieldAccuracyAsync(
        string merchantNormalized, string fieldName, CancellationToken ct = default)
    {
        var existing = await db.OcrFieldAccuracies
            .FirstOrDefaultAsync(a =>
                a.MerchantNameNormalized == merchantNormalized &&
                a.FieldName == fieldName, ct);

        if (existing is null)
        {
            db.OcrFieldAccuracies.Add(new OcrFieldAccuracy
            {
                MerchantNameNormalized = merchantNormalized,
                FieldName = fieldName,
                TotalExtractions = 1,
                TotalCorrections = 1,
            });
        }
        else
        {
            existing.TotalExtractions++;
            existing.TotalCorrections++;
            existing.LastUpdated = DateTimeOffset.UtcNow;
        }
    }

    public async Task<IReadOnlyList<OcrFieldAccuracy>> GetOcrAccuracyAsync(CancellationToken ct = default)
    {
        return await db.OcrFieldAccuracies
            .AsNoTracking()
            .OrderBy(a => a.MerchantNameNormalized)
            .ThenBy(a => a.FieldName)
            .ToListAsync(ct);
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
