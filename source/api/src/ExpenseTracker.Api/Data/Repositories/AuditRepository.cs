using ExpenseTracker.Audit.Entities;
using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data.Repositories;

/// <summary>
/// EF Core implementation of IAuditRepository.
///
/// RLS bypass note: The AppDbContext connects using the superuser credentials from
/// ConnectionStrings:DefaultConnection (the 'postgres' role), which bypasses PostgreSQL
/// Row Level Security. This means the Owner GET /audit endpoint can read all rows even
/// though the RLS policy only allows INSERT for application roles.
///
/// The RLS policy is set in migration 20260607000001_AuditAndExpenseTables.cs.
/// </summary>
internal sealed class AuditRepository(AppDbContext db) : IAuditRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken ct = default)
        => await db.AuditLogs.AddAsync(entry, ct);

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> QueryAsync(
        AuditLogQuery query,
        CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsNoTracking();

        if (query.UserId.HasValue)
            q = q.Where(e => e.UserId == query.UserId);

        if (query.From.HasValue)
            q = q.Where(e => e.CreatedAt >= query.From);

        if (query.To.HasValue)
            q = q.Where(e => e.CreatedAt <= query.To);

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(e => e.Action == query.Action);

        var total = await q.CountAsync(ct);

        var pageSize = query.PageSize;
        var skip = (query.Page - 1) * pageSize;

        var items = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
