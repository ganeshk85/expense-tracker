using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpenseTracker.Auth.Tests.Integration;

/// <summary>
/// Integration tests for MFA endpoints using an in-process test server.
/// These tests require a running PostgreSQL instance (or can be adapted with
/// a test container). They validate the HTTP layer and endpoint wiring.
/// </summary>
public sealed class MfaEndpointsTests : IClassFixture<MfaTestWebAppFactory>
{
    private readonly HttpClient _client;

    public MfaEndpointsTests(MfaTestWebAppFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Session cookies are HTTP-only; allow redirects so cookie jar works.
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task PostMfaSetup_WithoutSession_Returns401()
    {
        var response = await _client.PostAsync("/auth/mfa/setup", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostMfaLogin_WithoutPendingSession_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/mfa/login", new { code = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchAdminUsersMfa_WithoutSession_Returns401()
    {
        var userId = Guid.NewGuid();
        var response = await _client.PatchAsJsonAsync($"/admin/users/{userId}/mfa", new { enabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Configures a test web application with an in-memory database and a fixed MFA encryption key.
/// </summary>
public sealed class MfaTestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // 64-char hex key for AES-256 — for testing only
                ["Mfa:EncryptionKey"] = "AABBCCDDEEFF00112233445566778899AABBCCDDEEFF00112233445566778899",
                ["Internal:ApiKey"] = "test-internal-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the real DbContext with InMemory for isolation.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<ExpenseTracker.Api.Data.AppDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ExpenseTracker.Api.Data.AppDbContext>(opts =>
                opts.UseInMemoryDatabase("MfaTestDb_" + Guid.NewGuid()));
        });
    }
}
