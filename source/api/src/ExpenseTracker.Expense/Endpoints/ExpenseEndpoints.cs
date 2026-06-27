using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Expense.Endpoints;

public static class ExpenseEndpoints
{
    private const string SessionUserIdKey = "UserId";
    private const string SessionRoleKey = "Role";

    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/expenses")
            .WithTags("Expenses")
            .RequireAuthorization();

        group.MapPost("/", HandleCreate)
            .WithSummary("Create an expense manually (no receipt)");

        group.MapGet("/", HandleList)
            .WithSummary("List expenses for the current user");

        group.MapGet("/{id:guid}", HandleGetById)
            .WithSummary("Get a single expense by ID");

        group.MapPut("/{id:guid}", HandleUpdate)
            .WithSummary("Replace all editable fields on an expense");

        group.MapDelete("/{id:guid}", HandleDelete)
            .WithSummary("Delete an expense");

        group.MapPatch("/{id:guid}/corrections", HandleCorrect)
            .WithSummary("Apply OCR corrections to an expense");

        return app;
    }

    private static async Task<IResult> HandleCreate(
        CreateExpenseRequest request,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        var result = await service.CreateManualAsync(request, userId.Value, ct);
        return Results.Created($"/expenses/{result.Id}", result);
    }

    private static async Task<IResult> HandleList(
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct,
        bool allHousehold = false,
        int page = 1,
        int pageSize = 50)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.ListAsync(userId.Value, role, allHousehold, page, pageSize, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetById(
        Guid id,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.GetByIdAsync(id, userId.Value, role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdate(
        Guid id,
        UpdateExpenseRequest request,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.UpdateAsync(id, request, userId.Value, role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDelete(
        Guid id,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        await service.DeleteAsync(id, userId.Value, role, ip, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleCorrect(
        Guid id,
        CorrectExpenseRequest request,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var result = await service.ApplyCorrectionsAsync(id, request, userId.Value, role, ip, ct);
        return Results.Ok(result);
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.Session.GetString(SessionUserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
