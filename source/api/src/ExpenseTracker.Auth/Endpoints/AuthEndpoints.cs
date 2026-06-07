using ExpenseTracker.Audit.Middleware;
using ExpenseTracker.Auth.Models;
using ExpenseTracker.Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseTracker.Auth.Endpoints;

public static class AuthEndpoints
{
    private const string SessionUserIdKey = "UserId";
    private const string SessionRoleKey = "Role";
    private const string SessionMfaPendingKey = "MfaPending";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", HandleLogin)
            .AllowAnonymous()
            .WithSummary("Authenticate with username and password")
            .WithMetadata(new AuditedAttribute(action: "LOGIN", resourceType: "USER"));

        group.MapPost("/logout", HandleLogout)
            .RequireAuthorization()
            .WithSummary("Invalidate the current session")
            .WithMetadata(new AuditedAttribute(action: "LOGOUT", resourceType: "USER"));

        group.MapPost("/invite", HandleInvite)
            .RequireAuthorization("OwnerOnly")
            .WithSummary("Create an invite link for a new household member")
            .WithMetadata(new AuditedAttribute(action: "USER_INVITE", resourceType: "USER"));

        group.MapPost("/activate", HandleActivate)
            .AllowAnonymous()
            .WithSummary("Activate account using an invite token");

        group.MapPost("/mfa/setup", HandleMfaSetup)
            .RequireAuthorization()
            .WithSummary("Generate a TOTP secret and QR URI for MFA setup (does not persist)");

        group.MapPost("/mfa/enable", HandleMfaEnable)
            .RequireAuthorization()
            .WithSummary("Verify OTP against the setup secret and persist MFA for the current user");

        group.MapPost("/mfa/login", HandleMfaLogin)
            .AllowAnonymous()
            .WithSummary("Complete login with a TOTP code when MFA is required");

        var adminGroup = app.MapGroup("/admin").WithTags("Admin");

        adminGroup.MapMethods("/users/{id:guid}/mfa", ["PATCH"], HandleAdminMfaToggle)
            .RequireAuthorization("OwnerOnly")
            .WithSummary("Owner-only: enable or disable MFA for a household member")
            .WithMetadata(new AuditedAttribute(action: "MFA_CHANGE", resourceType: "USER"));

        return app;
    }

    private static async Task<IResult> HandleLogin(
        LoginRequest request,
        IAuthService authService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);

        if (result.MfaRequired)
        {
            // Store the authenticated user's ID in a pending-MFA session key.
            // Full session (UserId + Role) is only granted after OTP is verified.
            ctx.Session.SetString(SessionMfaPendingKey, result.UserId.ToString());
            return Results.Ok(new { mfaRequired = true });
        }

        ctx.Session.SetString(SessionUserIdKey, result.UserId.ToString());
        ctx.Session.SetString(SessionRoleKey, result.Role);
        return Results.Ok(result);
    }

    private static IResult HandleLogout(HttpContext ctx)
    {
        ctx.Session.Clear();
        return Results.NoContent();
    }

    private static async Task<IResult> HandleInvite(
        InviteRequest request,
        IAuthService authService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userIdStr = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var result = await authService.CreateInviteAsync(request, userId, ct);
        return Results.Created($"/auth/invite/{result.Token}", result);
    }

    private static async Task<IResult> HandleActivate(
        ActivateRequest request,
        IAuthService authService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var result = await authService.ActivateAccountAsync(request, ct);
        ctx.Session.SetString(SessionUserIdKey, result.UserId.ToString());
        ctx.Session.SetString(SessionRoleKey, result.Role);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleMfaSetup(
        IAuthService authService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userIdStr = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        var result = await authService.SetupMfaAsync(userId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleMfaEnable(
        MfaEnableRequest request,
        IAuthService authService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var userIdStr = ctx.Session.GetString(SessionUserIdKey);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Results.Problem("Session invalid.", statusCode: 401);

        await authService.EnableMfaAsync(userId, request, ct);
        return Results.Ok(new { message = "MFA has been enabled for your account." });
    }

    private static async Task<IResult> HandleMfaLogin(
        MfaLoginRequest request,
        IAuthService authService,
        HttpContext ctx,
        CancellationToken ct)
    {
        var pendingUserIdStr = ctx.Session.GetString(SessionMfaPendingKey);
        if (!Guid.TryParse(pendingUserIdStr, out var userId))
            return Results.Problem("No pending MFA session found. Please log in again.", statusCode: 400);

        var role = await authService.VerifyMfaLoginAsync(userId, request, ct);

        // Promote the pending session to a full authenticated session.
        ctx.Session.Remove(SessionMfaPendingKey);
        ctx.Session.SetString(SessionUserIdKey, userId.ToString());
        ctx.Session.SetString(SessionRoleKey, role);

        return Results.Ok(new { message = "MFA verified. Welcome." });
    }

    private static async Task<IResult> HandleAdminMfaToggle(
        Guid id,
        AdminMfaToggleRequest request,
        IAuthService authService,
        HttpContext ctx,
        CancellationToken ct)
    {
        // Authorization: OwnerOnly policy is enforced by RequireAuthorization("OwnerOnly").
        // The session handler returns 403 for non-owner roles before this handler is reached.
        await authService.AdminToggleMfaAsync(id, request.Enabled, ct);
        return Results.Ok(new { message = $"MFA {(request.Enabled ? "enabled" : "disabled")} for user {id}." });
    }
}
