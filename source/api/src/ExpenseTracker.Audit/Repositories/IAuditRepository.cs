using ExpenseTracker.Audit.Entities;
using ExpenseTracker.Audit.Models;

namespace ExpenseTracker.Audit.Repositories;

public interface IAuditRepository
{
    /// <summary>
    /// Appends a new audit log entry. Fire-and-forget callers should not await this
    /// on the hot path — use <see cref="IAuditService.LogAsync"/> which queues it.
    /// </summary>
    Task AddAsync(AuditLog entry, CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLog> Items, int Total)> QueryAsync(
        AuditLogQuery query,
        CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
