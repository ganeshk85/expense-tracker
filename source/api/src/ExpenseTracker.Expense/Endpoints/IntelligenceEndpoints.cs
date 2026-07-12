using ExpenseTracker.Audit.Models;
using ExpenseTracker.Audit.Services;
using ExpenseTracker.Expense.Models;
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
    private const string AdminRole = "Admin";

    public static IEndpointRouteBuilder MapIntelligenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/intelligence")
            .WithTags("Intelligence")
            .RequireAuthorization();

        group.MapGet("/merchant-map", GetMerchantMap);
        group.MapGet("/tag-suggestions", GetTagSuggestions);
        group.MapGet("/ocr-accuracy", GetOcrAccuracy);

        // ── Sprint 9 ──────────────────────────────────────────────────────────
        group.MapGet("/merchant-templates", GetMerchantTemplates);
        group.MapDelete("/merchant-templates/{merchantNormalized}", DeleteMerchantTemplates);

        group.MapGet("/recurring", GetRecurring);
        group.MapPost("/recurring/{id:guid}/snooze", SnoozeRecurring);

        group.MapGet("/merchant-aliases", GetMerchantAliases);
        group.MapPost("/merchant-aliases", CreateMerchantAlias);
        group.MapDelete("/merchant-aliases/{id:guid}", DeleteMerchantAlias);

        group.MapGet("/summary", GetSummary);

        // Intentionally excluded from the session-auth group — internal only,
        // called by the OCR worker after each confirmed receipt.
        var internalGroup = app.MapGroup("/internal/merchant-templates")
            .WithTags("Intelligence")
            .RequireAuthorization("InternalOnly");

        internalGroup.MapPost("/", PostInternalMerchantTemplate)
            .WithSummary("Upsert a merchant field-position template after a confirmed receipt (internal)");

        internalGroup.MapGet("/{merchantName}", GetInternalMerchantTemplates)
            .WithSummary("Fetch stored field-position templates for a merchant (internal, OCR worker)");

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

    // ── Sprint 9 handlers ────────────────────────────────────────────────────

    private static async Task<IResult> GetMerchantTemplates(
        HttpContext ctx, IIntelligenceService svc, IIntelligenceRepository repo, CancellationToken ct)
    {
        var role = ctx.Session.GetString(SessionRoleKey);
        if (role != AdminRole)
            return Results.Forbid();

        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var result = await svc.GetMerchantTemplatesAsync(householdId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteMerchantTemplates(
        string merchantNormalized,
        HttpContext ctx,
        IIntelligenceService svc,
        IIntelligenceRepository repo,
        IAuditService auditService,
        CancellationToken ct)
    {
        var role = ctx.Session.GetString(SessionRoleKey);
        if (role != AdminRole)
            return Results.Forbid();

        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var deleted = await svc.DeleteMerchantTemplatesAsync(householdId, merchantNormalized, ct);

        await auditService.LogAsync(new WriteAuditLogRequest(
            UserId: userId.Value,
            Action: "MERCHANT_TEMPLATE_DELETE",
            ResourceType: "MERCHANT_FIELD_TEMPLATE",
            ResourceId: null,
            BeforeJson: null,
            AfterJson: null,
            IpAddress: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown"), ct);

        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> PostInternalMerchantTemplate(
        UpsertMerchantTemplateRequest request,
        IIntelligenceService svc,
        IIntelligenceRepository repo,
        CancellationToken ct)
    {
        // Single-household deployment — GetHouseholdIdForUserAsync ignores the user id
        // and always resolves the one system household (see IntelligenceRepository).
        var householdId = await repo.GetHouseholdIdForUserAsync(Guid.Empty, ct);
        await svc.UpsertMerchantTemplateAsync(householdId, request, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetInternalMerchantTemplates(
        string merchantName,
        IIntelligenceService svc,
        IIntelligenceRepository repo,
        CancellationToken ct)
    {
        // Single-household deployment — GetHouseholdIdForUserAsync ignores the user id.
        var householdId = await repo.GetHouseholdIdForUserAsync(Guid.Empty, ct);
        var result = await svc.GetMerchantTemplatesForMerchantAsync(householdId, merchantName, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecurring(
        HttpContext ctx, IIntelligenceService svc, IIntelligenceRepository repo, CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var result = await svc.GetRecurringExpensesAsync(householdId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SnoozeRecurring(
        Guid id,
        HttpContext ctx,
        IIntelligenceService svc,
        IIntelligenceRepository repo,
        CancellationToken ct,
        int days = 30)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        await svc.SnoozeRecurringExpenseAsync(householdId, id, days, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetMerchantAliases(
        HttpContext ctx, IIntelligenceService svc, IIntelligenceRepository repo, CancellationToken ct)
    {
        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var result = await svc.GetAliasesAsync(householdId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateMerchantAlias(
        CreateMerchantAliasRequest request,
        HttpContext ctx,
        IIntelligenceService svc,
        IIntelligenceRepository repo,
        CancellationToken ct)
    {
        var role = ctx.Session.GetString(SessionRoleKey);
        if (role != AdminRole)
            return Results.Forbid();

        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var result = await svc.CreateAliasAsync(householdId, request, userId.Value, ct);
        return Results.Created("/intelligence/merchant-aliases", result);
    }

    private static async Task<IResult> DeleteMerchantAlias(
        Guid id, HttpContext ctx, IIntelligenceService svc, IIntelligenceRepository repo, CancellationToken ct)
    {
        var role = ctx.Session.GetString(SessionRoleKey);
        if (role != AdminRole)
            return Results.Forbid();

        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        await svc.DeleteAliasAsync(householdId, id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSummary(
        HttpContext ctx, IIntelligenceService svc, IIntelligenceRepository repo, CancellationToken ct)
    {
        var role = ctx.Session.GetString(SessionRoleKey);
        if (role != AdminRole)
            return Results.Forbid();

        var userId = GetUserId(ctx);
        if (userId is null)
            return Results.Problem("Session invalid.", statusCode: 401);

        var householdId = await repo.GetHouseholdIdForUserAsync(userId.Value, ct);
        var result = await svc.GetSummaryAsync(householdId, ct);
        return Results.Ok(result);
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var raw = ctx.Session.GetString(SessionUserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
