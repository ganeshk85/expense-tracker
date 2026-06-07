using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Audit.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/audit")
            .WithTags("Audit")
            .RequireAuthorization("OwnerOnly");

        group.MapGet("/", HandleGetLogs)
            .WithSummary("Retrieve paginated audit logs (Owner only)");

        return app;
    }

    private static async Task<IResult> HandleGetLogs(
        IAuditService auditService,
        Guid? userId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? action,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;

        var query = new AuditLogQuery(userId, from, to, action, page, pageSize);
        var result = await auditService.GetLogsAsync(query, ct);
        return Results.Ok(result);
    }
}
