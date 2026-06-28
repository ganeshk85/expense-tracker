using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Services;
using ExpenseTracker.Shared.Exceptions;
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

        // ── Expense CRUD ─────────────────────────────────────────────────────
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

        // ── Item CRUD ─────────────────────────────────────────────────────────
        group.MapGet("/{id:guid}/items", HandleGetItems)
            .WithSummary("List all line items for an expense");

        group.MapPost("/{id:guid}/items", HandleAddItem)
            .WithSummary("Add a line item to an expense");

        group.MapPut("/{id:guid}/items/{itemId:guid}", HandleUpdateItem)
            .WithSummary("Update a line item");

        group.MapDelete("/{id:guid}/items/{itemId:guid}", HandleDeleteItem)
            .WithSummary("Remove a line item from an expense");

        // ── Shared Expenses ───────────────────────────────────────────────────
        group.MapPost("/{id:guid}/shares", HandleAssignShares)
            .WithSummary("Assign or replace share splits for a shared expense");

        // ── Receipt Attachment ────────────────────────────────────────────────
        group.MapPost("/{id:guid}/receipts/{receiptId:guid}", HandleAttachReceipt)
            .WithSummary("Attach an already-uploaded receipt to an expense");

        group.MapDelete("/{id:guid}/receipts/{receiptId:guid}", HandleDetachReceipt)
            .WithSummary("Detach a receipt from an expense (does not delete the receipt file)");

        // ── File Attachments ──────────────────────────────────────────────────
        group.MapGet("/{id:guid}/attachments", HandleGetAttachments)
            .WithSummary("List file attachments for an expense");

        group.MapPost("/{id:guid}/attachments", HandleUploadAttachment)
            .WithSummary("Upload a file attachment to an expense")
            .DisableAntiforgery();

        group.MapDelete("/{id:guid}/attachments/{attachId:guid}", HandleDeleteAttachment)
            .WithSummary("Delete a file attachment");

        // ── Search ────────────────────────────────────────────────────────────
        group.MapGet("/search", HandleSearch)
            .WithSummary("Multi-field search across expenses");

        return app;
    }

    // ── Expense CRUD handlers ─────────────────────────────────────────────────

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

        try
        {
            var result = await service.UpdateAsync(id, request, userId.Value, role, ct);
            return Results.Ok(result);
        }
        catch (ConflictException ex) when (ex.Message == "shares_out_of_sync")
        {
            return Results.Conflict(new { sharesOutOfSync = true });
        }
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

    // ── Item CRUD handlers ────────────────────────────────────────────────────

    private static async Task<IResult> HandleGetItems(
        Guid id,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.GetItemsAsync(id, userId.Value, role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleAddItem(
        Guid id,
        CreateExpenseItemRequest request,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.AddItemAsync(id, request, userId.Value, role, ct);
        return Results.Created($"/expenses/{id}/items/{result.Id}", result);
    }

    private static async Task<IResult> HandleUpdateItem(
        Guid id,
        Guid itemId,
        CreateExpenseItemRequest request,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.UpdateItemAsync(id, itemId, request, userId.Value, role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteItem(
        Guid id,
        Guid itemId,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        await service.DeleteItemAsync(id, itemId, userId.Value, role, ct);
        return Results.NoContent();
    }

    // ── Shared Expense handlers ───────────────────────────────────────────────

    private static async Task<IResult> HandleAssignShares(
        Guid id,
        AssignSharesRequest request,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.AssignSharesAsync(id, request, userId.Value, role, ct);
        return Results.Ok(result);
    }

    // ── Receipt Attachment handlers ───────────────────────────────────────────

    private static async Task<IResult> HandleAttachReceipt(
        Guid id,
        Guid receiptId,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.AttachReceiptAsync(id, receiptId, userId.Value, role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDetachReceipt(
        Guid id,
        Guid receiptId,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        await service.DetachReceiptAsync(id, receiptId, userId.Value, role, ct);
        return Results.NoContent();
    }

    // ── File Attachment handlers ──────────────────────────────────────────────

    private static async Task<IResult> HandleGetAttachments(
        Guid id,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.GetAttachmentsAsync(id, userId.Value, role, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUploadAttachment(
        Guid id,
        IFormFile file,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var result = await service.AddAttachmentAsync(id, file, userId.Value, role, ct);
        return Results.Created($"/expenses/{id}/attachments/{result.Id}", result);
    }

    private static async Task<IResult> HandleDeleteAttachment(
        Guid id,
        Guid attachId,
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        await service.DeleteAttachmentAsync(id, attachId, userId.Value, role, ct);
        return Results.NoContent();
    }

    // ── Search handler ────────────────────────────────────────────────────────

    private static async Task<IResult> HandleSearch(
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct,
        string? q = null,
        string? category = null,
        string? merchant = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string[]? tags = null,
        int page = 1,
        int pageSize = 50)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        var request = new SearchExpensesRequest(q, category, merchant, dateFrom, dateTo,
            minAmount, maxAmount, tags, page, pageSize);

        var result = await service.SearchAsync(request, userId.Value, role, ct);
        return Results.Ok(result);
    }

    // ── Session helpers ───────────────────────────────────────────────────────

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.Session.GetString(SessionUserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
