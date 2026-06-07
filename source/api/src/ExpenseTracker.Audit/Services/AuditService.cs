using ExpenseTracker.Audit.Entities;
using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Audit.Services;

/// <summary>
/// Writes audit log entries asynchronously without blocking the originating request.
/// A new DI scope is created per write so the short-lived AppDbContext does not
/// escape its original request scope.
/// </summary>
public sealed class AuditService(
    IServiceScopeFactory scopeFactory,
    ILogger<AuditService> logger) : IAuditService
{
    public Task LogAsync(WriteAuditLogRequest request, CancellationToken ct = default)
    {
        // Fire-and-forget: do not await; audit failure must never surface to the caller.
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IAuditRepository>();

                var entry = new AuditLog
                {
                    UserId = request.UserId,
                    Action = request.Action,
                    ResourceType = request.ResourceType,
                    ResourceId = request.ResourceId,
                    BeforeJson = request.BeforeJson,
                    AfterJson = request.AfterJson,
                    IpAddress = string.IsNullOrWhiteSpace(request.IpAddress)
                        ? "unknown"
                        : request.IpAddress,
                };

                await repo.AddAsync(entry, CancellationToken.None);
                await repo.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Audit failures are logged but never re-thrown — must not impact the caller.
                logger.LogError(ex, "Failed to write audit log for action {Action}", request.Action);
            }
        }, ct);

        return Task.CompletedTask;
    }

    public async Task<AuditLogPagedResponse> GetLogsAsync(
        AuditLogQuery query,
        CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAuditRepository>();

        var (items, total) = await repo.QueryAsync(query, ct);

        var dtoItems = items
            .Select(e => new AuditLogItem(
                e.Id,
                e.UserId,
                e.Action,
                e.ResourceType,
                e.ResourceId,
                e.BeforeJson,
                e.AfterJson,
                e.IpAddress,
                e.CreatedAt))
            .ToList()
            .AsReadOnly();

        return new AuditLogPagedResponse(dtoItems, total, query.Page, query.PageSize);
    }
}
