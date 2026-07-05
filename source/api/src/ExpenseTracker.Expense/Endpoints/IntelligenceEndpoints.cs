using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Expense.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Expense.Endpoints;

public static class IntelligenceEndpoints
{
    private const string SessionUserIdKey = "UserId";
    private const string SessionRoleKey = "Role";

    public static IEndpointRouteBuilder MapIntelligenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/intelligence")
            .WithTags("Intelligence")
            .RequireAuthorization();

        group.MapGet("/merchant-map", GetMerchantMap);
        group.MapGet("/tag-suggestions", GetTagSuggestions);
        group.MapGet("/ocr-accuracy", GetOcrAccuracy);

        return app;
    }

    private static async Task<IResult> GetMerchantMap(
        HttpContext ctx,
        IIntelligenceService svc,
        IIntelligenceRepository repo,
        CancellationToken ct)
    {
        var role = ctx.Session.GetString(SessionRoleKey);
        if (role != "Admin")
            return Results.Forbid();

        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var result = await svc.GetMerchantCategoryMapAsync(householdId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTagSuggestions(
        HttpContext ctx,
        IIntelligenceService svc,
        IIntelligenceRepository repo,
        string? merchant,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var result = await svc.GetTagSuggestionsAsync(householdId, merchant, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetOcrAccuracy(
        HttpContext ctx,
        IIntelligenceService svc,
        CancellationToken ct)
    {
        var role = ctx.Session.GetString(SessionRoleKey);
        if (role != "Admin")
            return Results.Forbid();

        var result = await svc.GetOcrAccuracyAsync(ct);
        return Results.Ok(result);
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.Session.GetString(SessionUserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
