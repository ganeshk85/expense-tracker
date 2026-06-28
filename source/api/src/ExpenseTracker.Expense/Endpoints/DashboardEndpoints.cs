using ExpenseTracker.Expense.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Expense.Endpoints;

public static class DashboardEndpoints
{
    private const string SessionUserIdKey = "UserId";
    private const string SessionRoleKey = "Role";

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", HandleSummary)
            .WithSummary("Get spending summary for a given month");

        return app;
    }

    private static async Task<IResult> HandleSummary(
        string? month,
        string? view,
        IDashboardService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var rawId = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(rawId, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;
        var targetMonth = month ?? DateTime.UtcNow.ToString("yyyy-MM");
        var household = string.Equals(view, "household", StringComparison.OrdinalIgnoreCase);

        var result = await service.GetSummaryAsync(userId, role, targetMonth, household, ct);
        return Results.Ok(result);
    }
}
