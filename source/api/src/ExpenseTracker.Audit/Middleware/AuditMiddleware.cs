using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Audit.Middleware;

/// <summary>
/// ASP.NET Core middleware that intercepts all mutating HTTP methods (POST, PUT, PATCH, DELETE)
/// and emits an audit log entry after the response is written.
///
/// Endpoints opt in by attaching <see cref="AuditedAttribute"/> via endpoint metadata.
/// Endpoints without the attribute are skipped to avoid audit noise on non-critical paths.
///
/// The audit entry is written asynchronously (fire-and-forget inside AuditService)
/// so it does not add latency to the response.
/// </summary>
public sealed class AuditMiddleware(
    RequestDelegate next,
    ILogger<AuditMiddleware> logger)
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private const string SessionUserIdKey = "UserId";

    public async Task InvokeAsync(HttpContext ctx, IAuditService auditService)
    {
        await next(ctx);

        // Only audit mutating methods on opted-in endpoints.
        if (!MutatingMethods.Contains(ctx.Request.Method))
            return;

        var endpointFeature = ctx.Features.Get<IEndpointFeature>();
        var attribute = endpointFeature?.Endpoint?.Metadata.GetMetadata<AuditedAttribute>();
        if (attribute is null)
            return;

        // Resolve user from session (may be null for anonymous endpoints like /auth/activate).
        Guid? userId = null;
        var userIdStr = ctx.Session.GetString(SessionUserIdKey);
        if (Guid.TryParse(userIdStr, out var parsedUserId))
            userId = parsedUserId;

        var ipAddress = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            await auditService.LogAsync(new WriteAuditLogRequest(
                UserId: userId,
                Action: attribute.Action,
                ResourceType: attribute.ResourceType,
                IpAddress: ipAddress));
        }
        catch (Exception ex)
        {
            // Middleware must not surface audit errors to the caller.
            logger.LogError(ex, "AuditMiddleware failed to enqueue log for action {Action}",
                attribute.Action);
        }
    }
}
