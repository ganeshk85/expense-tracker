using ExpenseTracker.Expense.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Expense.Endpoints;

public static class AnalyticsEndpoints
{
    private const string SessionUserIdKey = "UserId";
    private const string SessionRoleKey = "Role";

    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/analytics")
            .WithTags("Analytics")
            .RequireAuthorization();

        group.MapGet("/category-trends", HandleCategoryTrends)
            .WithSummary("Monthly spending per category for the last N months");

        group.MapGet("/merchants", HandleMerchantRankings)
            .WithSummary("Ranked list of merchants by total spend");

        group.MapGet("/merchants/{name}", HandleMerchantDetail)
            .WithSummary("All expenses for a specific merchant");

        return app;
    }

    private static async Task<IResult> HandleCategoryTrends(
        int? months, string? category,
        IAnalyticsService service, HttpContext ctx, CancellationToken ct)
    {
        var rawId = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(rawId, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;
        var result = await service.GetCategoryTrendsAsync(userId, role, months ?? 6, category, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleMerchantRankings(
        string? dateFrom, string? dateTo,
        IAnalyticsService service, HttpContext ctx, CancellationToken ct)
    {
        var rawId = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(rawId, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;
        DateTimeOffset? from = DateTimeOffset.TryParse(dateFrom, out var df) ? df : null;
        DateTimeOffset? to = DateTimeOffset.TryParse(dateTo, out var dt) ? dt : null;

        var result = await service.GetMerchantRankingsAsync(userId, role, from, to, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleMerchantDetail(
        string name, string? dateFrom, string? dateTo,
        IAnalyticsService service, HttpContext ctx, CancellationToken ct)
    {
        var rawId = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(rawId, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;
        DateTimeOffset? from = DateTimeOffset.TryParse(dateFrom, out var df) ? df : null;
        DateTimeOffset? to = DateTimeOffset.TryParse(dateTo, out var dt) ? dt : null;

        var result = await service.GetMerchantDetailAsync(userId, role, name, from, to, ct);
        return Results.Ok(result);
    }
}
