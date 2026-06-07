using ExpenseTracker.Audit.Middleware;
using ExpenseTracker.Receipt.Models;
using ExpenseTracker.Receipt.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Receipt.Endpoints;

public static class ReceiptEndpoints
{
    private const string SessionUserIdKey = "UserId";

    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/receipts")
            .WithTags("Receipts")
            .RequireAuthorization();

        group.MapPost("/upload", HandleUpload)
            .DisableAntiforgery()
            .WithSummary("Upload a receipt image or PDF")
            .WithMetadata(new AuditedAttribute(action: "RECEIPT_UPLOAD", resourceType: "RECEIPT"));

        group.MapGet("/{id:guid}/status", HandleGetStatus)
            .WithSummary("Poll OCR processing status for a receipt");

        // Internal endpoint — accessible only by the OCR worker via X-Internal-Key header.
        // Intentionally excluded from the session-auth group to prevent user access.
        var internalGroup = app.MapGroup("/receipts")
            .WithTags("Receipts")
            .RequireAuthorization("InternalOnly");

        internalGroup.MapPatch("/{id:guid}/thumbnail", HandleUpdateThumbnail)
            .WithSummary("Update thumbnail path after generation (internal)");

        return app;
    }

    private static async Task<IResult> HandleUpload(
        IFormFile file,
        IReceiptService receiptService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userIdStr = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var result = await receiptService.UploadAsync(file, userId, ct);
        return Results.Created($"/receipts/{result.ReceiptId}/status", result);
    }

    private static async Task<IResult> HandleGetStatus(
        Guid id,
        IReceiptService receiptService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userIdStr = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var result = await receiptService.GetStatusAsync(id, userId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateThumbnail(
        Guid id,
        UpdateThumbnailRequest request,
        IReceiptService receiptService,
        CancellationToken ct)
    {
        await receiptService.UpdateThumbnailAsync(id, request, ct);
        return Results.NoContent();
    }
}
