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
        string merchantNormalized, string fieldName, bool isCorrected, CancellationToken ct = default)
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
                TotalCorrections = isCorrected ? 1 : 0,
            });
        }
        else
        {
            existing.TotalExtractions++;
            if (isCorrected)
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

    // ── Merchant field templates (US-INT-05) ──────────────────────────────────

    public async Task UpsertMerchantTemplateAsync(
        Guid householdId, string merchantNormalized, string fieldName,
        double regionX, double regionY, double regionW, double regionH, CancellationToken ct = default)
    {
        var existing = await db.MerchantFieldTemplates
            .FirstOrDefaultAsync(t =>
                t.HouseholdId == householdId &&
                t.MerchantNameNormalized == merchantNormalized &&
                t.FieldName == fieldName, ct);

        if (existing is null)
        {
            db.MerchantFieldTemplates.Add(new MerchantFieldTemplate
            {
                HouseholdId = householdId,
                MerchantNameNormalized = merchantNormalized,
                FieldName = fieldName,
                RegionX = regionX,
                RegionY = regionY,
                RegionW = regionW,
                RegionH = regionH,
                SampleCount = 1,
            });
        }
        else
        {
            // Weighted moving average: (existing * sampleCount + new) / (sampleCount + 1).
            var n = existing.SampleCount;
            existing.RegionX = (existing.RegionX * n + regionX) / (n + 1);
            existing.RegionY = (existing.RegionY * n + regionY) / (n + 1);
            existing.RegionW = (existing.RegionW * n + regionW) / (n + 1);
            existing.RegionH = (existing.RegionH * n + regionH) / (n + 1);
            existing.SampleCount++;
            existing.LastUpdated = DateTimeOffset.UtcNow;
        }
    }

    public async Task<IReadOnlyList<MerchantFieldTemplate>> GetMerchantTemplatesAsync(
        Guid householdId, CancellationToken ct = default)
    {
        return await db.MerchantFieldTemplates
            .AsNoTracking()
            .Where(t => t.HouseholdId == householdId)
            .OrderBy(t => t.MerchantNameNormalized).ThenBy(t => t.FieldName)
            .ToListAsync(ct);
    }

    public async Task<MerchantFieldTemplate?> FindMerchantTemplateAsync(
        Guid householdId, string merchantNormalized, string fieldName, CancellationToken ct = default)
    {
        return await db.MerchantFieldTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.HouseholdId == householdId &&
                t.MerchantNameNormalized == merchantNormalized &&
                t.FieldName == fieldName, ct);
    }

    public async Task<int> DeleteMerchantTemplatesAsync(
        Guid householdId, string merchantNormalized, CancellationToken ct = default)
    {
        var rows = await db.MerchantFieldTemplates
            .Where(t => t.HouseholdId == householdId && t.MerchantNameNormalized == merchantNormalized)
            .ToListAsync(ct);

        db.MerchantFieldTemplates.RemoveRange(rows);
        return rows.Count;
    }

    // ── Recurring expenses (US-INT-06) ─────────────────────────────────────────

    public async Task<IReadOnlyList<RecurringExpense>> GetRecurringExpensesAsync(
        Guid householdId, CancellationToken ct = default)
    {
        return await db.RecurringExpenses
            .AsNoTracking()
            .Where(r => r.HouseholdId == householdId)
            .OrderBy(r => r.MerchantNameNormalized)
            .ToListAsync(ct);
    }

    public async Task<RecurringExpense?> FindRecurringExpenseAsync(
        Guid householdId, Guid id, CancellationToken ct = default)
    {
        return await db.RecurringExpenses
            .FirstOrDefaultAsync(r => r.HouseholdId == householdId && r.Id == id, ct);
    }

    public async Task SnoozeRecurringExpenseAsync(
        Guid householdId, Guid id, int days, CancellationToken ct = default)
    {
        var entry = await db.RecurringExpenses
            .FirstOrDefaultAsync(r => r.HouseholdId == householdId && r.Id == id, ct);

        if (entry is not null)
            entry.SnoozedUntil = DateTimeOffset.UtcNow.AddDays(days);
    }

    public async Task DetectRecurringExpensesAsync(Guid householdId, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddMonths(-6);

        // Expenses have no household FK yet (single-household deployment — see SystemHouseholdId
        // above), so every expense belongs to the household being scanned.
        // Pull the raw rows needed for grouping; the merchant/amount/month bucketing itself
        // is done in memory since it needs per-group statistics EF can't express in one query.
        var rows = await db.Expenses
            .AsNoTracking()
            .Where(e =>
                e.Date.HasValue &&
                e.Date.Value >= since &&
                e.MerchantName != null &&
                e.Total.HasValue)
            .Select(e => new { e.MerchantName, e.Total, e.Date })
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var recentMonths = Enumerable.Range(0, 4)
            .Select(i => new DateOnly(now.Year, now.Month, 1).AddMonths(-i))
            .ToList();

        var byMerchant = rows
            .Select(r => new
            {
                Normalized = ExpenseTracker.Shared.MerchantNormalizer.Normalize(r.MerchantName),
                r.Total,
                Month = new DateOnly(r.Date!.Value.Year, r.Date.Value.Month, 1),
                Day = r.Date.Value.Day,
            })
            .Where(r => !string.IsNullOrEmpty(r.Normalized))
            .GroupBy(r => r.Normalized);

        foreach (var group in byMerchant)
        {
            // Cluster by amount within 5% so slightly-varying recurring bills (e.g. utilities) still match.
            var amountClusters = new List<List<(decimal Amount, DateOnly Month, int Day)>>();
            foreach (var item in group)
            {
                var amount = item.Total!.Value;
                var cluster = amountClusters.FirstOrDefault(c => Math.Abs(c[0].Amount - amount) <= c[0].Amount * 0.05m);
                if (cluster is null)
                {
                    cluster = [];
                    amountClusters.Add(cluster);
                }
                cluster.Add((amount, item.Month, item.Day));
            }

            foreach (var cluster in amountClusters)
            {
                var monthsPresent = cluster.Select(c => c.Month).Distinct()
                    .Count(m => recentMonths.Contains(m));

                if (monthsPresent < 3)
                    continue;

                var confidence = monthsPresent >= 4 ? "confirmed" : "likely";
                var averageAmount = cluster.Average(c => c.Amount);
                var typicalDay = (int)cluster.Select(c => (double)c.Day).OrderBy(d => d)
                    .ElementAt(cluster.Count / 2);

                var existing = await db.RecurringExpenses.FirstOrDefaultAsync(
                    r => r.HouseholdId == householdId && r.MerchantNameNormalized == group.Key, ct);

                if (existing is null)
                {
                    db.RecurringExpenses.Add(new RecurringExpense
                    {
                        HouseholdId = householdId,
                        MerchantNameNormalized = group.Key,
                        AverageAmount = averageAmount,
                        TypicalDayOfMonth = typicalDay,
                        Confidence = confidence,
                        LastDetectedAt = DateTimeOffset.UtcNow,
                    });
                }
                else
                {
                    existing.AverageAmount = averageAmount;
                    existing.TypicalDayOfMonth = typicalDay;
                    existing.Confidence = confidence;
                    existing.LastDetectedAt = DateTimeOffset.UtcNow;
                }
            }
        }
    }

    // ── Merchant aliases (US-INT-07) ───────────────────────────────────────────

    public async Task<string> ResolveAliasAsync(
        Guid householdId, string merchantNormalized, CancellationToken ct = default)
    {
        var alias = await db.MerchantAliases
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.HouseholdId == householdId && a.AliasNormalized == merchantNormalized, ct);

        return alias?.CanonicalNormalized ?? merchantNormalized;
    }

    public async Task<MerchantAlias> CreateAliasAsync(
        Guid householdId, string aliasNormalized, string canonicalNormalized, Guid createdBy, CancellationToken ct = default)
    {
        var entity = new MerchantAlias
        {
            HouseholdId = householdId,
            AliasNormalized = aliasNormalized,
            CanonicalNormalized = canonicalNormalized,
            CreatedBy = createdBy,
        };
        db.MerchantAliases.Add(entity);
        return entity;
    }

    public async Task<IReadOnlyList<MerchantAlias>> GetAliasesAsync(
        Guid householdId, CancellationToken ct = default)
    {
        return await db.MerchantAliases
            .AsNoTracking()
            .Where(a => a.HouseholdId == householdId)
            .OrderBy(a => a.AliasNormalized)
            .ToListAsync(ct);
    }

    public async Task DeleteAliasAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var entity = await db.MerchantAliases
            .FirstOrDefaultAsync(a => a.HouseholdId == householdId && a.Id == id, ct);

        if (entity is not null)
            db.MerchantAliases.Remove(entity);
    }

    // ── Intelligence summary (US-INT-08) ───────────────────────────────────────

    public async Task<(int MerchantMappings, int FieldTemplates, int RecurringExpenses, int Aliases)> GetSummaryCountsAsync(
        Guid householdId, CancellationToken ct = default)
    {
        var merchantMappings = await db.MerchantCategoryMaps.CountAsync(m => m.HouseholdId == householdId, ct);
        var fieldTemplates = await db.MerchantFieldTemplates.CountAsync(t => t.HouseholdId == householdId, ct);
        var recurringExpenses = await db.RecurringExpenses.CountAsync(r => r.HouseholdId == householdId, ct);
        var aliases = await db.MerchantAliases.CountAsync(a => a.HouseholdId == householdId, ct);

        return (merchantMappings, fieldTemplates, recurringExpenses, aliases);
    }
}
