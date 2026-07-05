using System.Text;
using ExpenseTracker.Audit.Services;
using ExpenseTracker.Expense.Models;
using ExpenseTracker.Expense.Services;
using ExpenseTracker.Shared;
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

        // ── Export ────────────────────────────────────────────────────────────
        group.MapGet("/export", HandleExport)
            .WithSummary("Stream expenses as CSV for the given date range");

        // ── Intelligence ──────────────────────────────────────────────────────
        group.MapPost("/{id:guid}/dismiss-duplicate", HandleDismissDuplicate)
            .WithSummary("Dismiss a duplicate warning for this expense — suppresses future warnings");

        return app;
    }

    // ── Expense CRUD handlers ─────────────────────────────────────────────────

    private static async Task<IResult> HandleCreate(
        CreateExpenseRequest request,
        IExpenseService service,
        IBudgetAlertService? alertService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);

        var result = await service.CreateManualAsync(request, userId.Value, ct);

        if (alertService is not null && request.Category is not null)
            await alertService.CheckAndFireAlertsAsync(userId.Value, request.Category, ct);

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
        IBudgetAlertService? alertService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        try
        {
            var result = await service.UpdateAsync(id, request, userId.Value, role, ct);

            if (alertService is not null && request.Category is not null)
                await alertService.CheckAndFireAlertsAsync(userId.Value, request.Category, ct);

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

    // ── Export handler ────────────────────────────────────────────────────────

    private static async Task<IResult> HandleExport(
        IExpenseService service,
        HttpContext ctx,
        CancellationToken ct,
        string? from = null,
        string? to = null)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var role = ctx.Session.GetString(SessionRoleKey) ?? string.Empty;

        DateTimeOffset? fromDate = from is not null && DateTimeOffset.TryParse(from, out var f) ? f : null;
        DateTimeOffset? toDate = to is not null && DateTimeOffset.TryParse(to, out var t) ? t.AddDays(1).AddSeconds(-1) : null;

        var expenses = await service.ExportAsync(userId.Value, role, fromDate, toDate, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Merchant,Category,Tags,Amount,Currency,Source,Notes");

        foreach (var e in expenses)
        {
            var date = e.Date.HasValue ? e.Date.Value.ToString("yyyy-MM-dd") : string.Empty;
            var merchant = CsvEscape(e.MerchantName ?? string.Empty);
            var category = CsvEscape(e.Category ?? string.Empty);
            var tags = CsvEscape(e.Tags is { Length: > 0 } ? string.Join(";", e.Tags) : string.Empty);
            var amount = e.Total.HasValue ? e.Total.Value.ToString("F2") : "0.00";
            var source = CsvEscape(e.Source?.ToString() ?? string.Empty);
            var notes = CsvEscape(e.Notes ?? string.Empty);

            sb.AppendLine($"{date},{merchant},{category},{tags},{amount},USD,{source},{notes}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var filename = $"expenses-{from ?? "all"}-{to ?? "all"}.csv";

        return Results.File(bytes, "text/csv", filename);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // ── Intelligence handlers ─────────────────────────────────────────────────

    private static async Task<IResult> HandleDismissDuplicate(
        Guid id,
        IIntelligenceService intelligenceService,
        IAuditService auditService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null) return Results.Problem("Session invalid.", statusCode: 401);
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        await intelligenceService.DismissDuplicateAsync(id, userId.Value, ct);

        await auditService.LogAsync(new ExpenseTracker.Audit.Models.WriteAuditLogRequest(
            UserId: userId.Value,
            Action: "DUPLICATE_DISMISS",
            ResourceType: "EXPENSE",
            ResourceId: id,
            BeforeJson: null,
            AfterJson: null,
            IpAddress: ip), ct);

        return Results.NoContent();
    }

    // ── Session helpers ───────────────────────────────────────────────────────

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.Session.GetString(SessionUserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
