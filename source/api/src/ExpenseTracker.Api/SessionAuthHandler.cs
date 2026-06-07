using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ExpenseTracker.Api;

public sealed class SessionAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Session";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Context.Session.LoadAsync(Context.RequestAborted);

        var userId = Context.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return AuthenticateResult.NoResult();

        var role = Context.Session.GetString("Role") ?? string.Empty;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("Role", role),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
