using ExpenseTracker.Budget.Models;
using ExpenseTracker.Budget.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Budget.Endpoints;

public static class BudgetEndpoints
{
    private const string SessionUserIdKey = "UserId";

    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/budgets")
            .WithTags("Budgets")
            .RequireAuthorization();

        group.MapPost("/", HandleCreate)
            .WithSummary("Create a monthly category budget");

        group.MapGet("/", HandleList)
            .WithSummary("List all budgets for the current user");

        group.MapPut("/{id:guid}", HandleUpdate)
            .WithSummary("Update the monthly limit for a budget");

        group.MapDelete("/{id:guid}", HandleDelete)
            .WithSummary("Delete a budget");

        return app;
    }

    private static async Task<IResult> HandleCreate(
        CreateBudgetRequest request,
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        var result = await service.CreateAsync(request, userId.Value, ct);
        return Results.Created($"/budgets/{result.Id}", result);
    }

    private static async Task<IResult> HandleList(
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        var result = await service.ListAsync(userId.Value, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdate(
        Guid id,
        UpdateBudgetRequest request,
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        var result = await service.UpdateAsync(id, request, userId.Value, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(
        Guid id,
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        await service.DeleteAsync(id, userId.Value, ct);
        return Results.NoContent();
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.Session.GetString(SessionUserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
