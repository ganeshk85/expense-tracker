using ExpenseTracker.Audit.Models;

namespace ExpenseTracker.Audit.Services;

public interface IAuditService
{
    /// <summary>
    /// Enqueues an audit log write asynchronously so callers are not blocked.
    /// Uses a background fire-and-forget pattern; errors are logged but never propagate.
    /// </summary>
    Task LogAsync(WriteAuditLogRequest request, CancellationToken ct = default);

    Task<AuditLogPagedResponse> GetLogsAsync(AuditLogQuery query, CancellationToken ct = default);
}
