using ExpenseTracker.Budget.Models;
using ExpenseTracker.Budget.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Budget.Endpoints;

public static class BudgetEndpoints
{
    private const string SessionUserIdKey = "UserId";
    private const string SessionRoleKey = "Role";

    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var budgets = app.MapGroup("/budgets")
            .WithTags("Budgets")
            .RequireAuthorization();

        budgets.MapPost("/", HandleCreate)
            .WithSummary("Create a monthly category or household budget");

        budgets.MapGet("/", HandleList)
            .WithSummary("List all budgets for the current user with progress");

        budgets.MapPut("/{id:guid}", HandleUpdate)
            .WithSummary("Update the monthly limit for a budget");

        budgets.MapDelete("/{id:guid}", HandleDelete)
            .WithSummary("Delete a budget");

        budgets.MapGet("/history", HandleHistory)
            .WithSummary("Get budget history snapshots for a given month (YYYY-MM)");

        var notifications = app.MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        notifications.MapGet("/", HandleGetNotifications)
            .WithSummary("List unread budget notifications for the current user");

        notifications.MapPost("/{id:guid}/dismiss", HandleDismiss)
            .WithSummary("Dismiss a notification");

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
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.CreateAsync(request, userId.Value, role, ct);
        return Results.Created($"/budgets/{result.Id}", result);
    }

    private static async Task<IResult> HandleList(
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.ListAsync(userId.Value, role, ct);
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

    private static async Task<IResult> HandleHistory(
        string? month,
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        var targetMonth = month ?? DateTime.UtcNow.ToString("yyyy-MM");
        var result = await service.GetHistoryAsync(userId.Value, targetMonth, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetNotifications(
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        var result = await service.GetNotificationsAsync(userId.Value, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDismiss(
        Guid id,
        IBudgetService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        await service.DismissNotificationAsync(id, userId.Value, ct);
        return Results.NoContent();
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.Session.GetString(SessionUserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
