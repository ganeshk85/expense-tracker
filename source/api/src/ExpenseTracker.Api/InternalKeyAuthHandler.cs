using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ExpenseTracker.Api;

public sealed class InternalKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "InternalKey";
    private const string HeaderName = "X-Internal-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expectedKey = configuration["Internal:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return Task.FromResult(AuthenticateResult.Fail("Internal API key is not configured."));

        if (!Request.Headers.TryGetValue(HeaderName, out var providedKey))
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Internal-Key header."));

        if (!string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key."));

        var claims = new[] { new Claim(ClaimTypes.Name, "InternalWorker") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
