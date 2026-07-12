using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace ExpenseTracker.Budget.Tests.Integration;

/// <summary>
/// Integration tests for Sprint 6 (budget history, dashboard, CSV export)
/// and Sprint 7 (analytics) endpoints.
/// Uses an in-memory database and real session authentication via POST /auth/login.
/// </summary>

// ── Budget history (Sprint 6 carry-over) ─────────────────────────────────────

public sealed class BudgetHistoryEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public BudgetHistoryEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetBudgetHistory_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/budgets/history");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBudgetHistory_AsAdmin_Returns200WithItems()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/budgets/history");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BudgetHistoryBody>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBudgetHistory_AsContributor_Returns403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/budgets/history");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record BudgetHistoryBody(IReadOnlyList<object> Items);
}

// ── Dashboard summary (Sprint 6 carry-over) ───────────────────────────────────

public sealed class DashboardSummaryEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public DashboardSummaryEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetDashboardSummary_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboardSummary_WithSession_Returns200WithExpectedShape()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/dashboard/summary?month=2026-08");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardBody>();
        body.Should().NotBeNull();
        body!.Month.Should().Be("2026-08");
        body.TotalSpent.Should().Be(0);
        body.CategoryBreakdown.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardSummary_InvalidMonth_Returns422()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/dashboard/summary?month=not-a-month");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record DashboardBody(
        string Month,
        decimal TotalSpent,
        int ExpenseCount,
        IReadOnlyList<object> CategoryBreakdown,
        IReadOnlyList<object> TopMerchants);
}

// ── CSV export (Sprint 6 carry-over) ─────────────────────────────────────────

public sealed class ExpenseExportEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public ExpenseExportEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetExpensesExport_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/expenses/export");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExpensesExport_WithSession_ReturnsCsvContentType()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/expenses/export");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task GetExpensesExport_WithSession_ReturnsHeaderRow()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/expenses/export");
        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().StartWith("Date,");
        csv.Should().Contain("Merchant");
        csv.Should().Contain("Amount");
    }
}

// ── Category trends (Sprint 7) ────────────────────────────────────────────────

public sealed class CategoryTrendEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public CategoryTrendEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetCategoryTrends_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/analytics/category-trends");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategoryTrends_WithSession_Returns200WithExpectedShape()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/analytics/category-trends?months=6");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CategoryTrendBody>();
        body.Should().NotBeNull();
        body!.Months.Should().HaveCount(6);
        body.Series.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCategoryTrends_InvalidMonths_Returns422()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/analytics/category-trends?months=99");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record CategoryTrendBody(
        IReadOnlyList<string> Months,
        IReadOnlyList<object> Series);
}

// ── Merchant analytics (Sprint 7) ────────────────────────────────────────────

public sealed class MerchantAnalyticsEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public MerchantAnalyticsEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMerchants_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/analytics/merchants");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMerchants_WithSession_Returns200WithRankedList()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/analytics/merchants");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MerchantsBody>();
        body.Should().NotBeNull();
        body!.Merchants.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMerchantDetail_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/analytics/merchants/Woolworths");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMerchantDetail_WithSession_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/analytics/merchants/Woolworths");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MerchantDetailBody>();
        body.Should().NotBeNull();
        body!.Merchant.Should().Be("Woolworths");
        body.Expenses.Should().NotBeNull();
    }

    private sealed record MerchantsBody(IReadOnlyList<object> Merchants);

    private sealed record MerchantDetailBody(
        string Merchant,
        decimal TotalSpent,
        int VisitCount,
        IReadOnlyList<object> Expenses);
}

// ── Shared test factory ───────────────────────────────────────────────────────

/// <summary>
/// Shared test web application factory for Sprint 6 + 7 integration tests.
/// Uses an in-memory database. Provides a helper to create pre-authenticated
/// HTTP clients by seeding a user and going through the real login flow.
/// </summary>
public sealed class Sprint6And7WebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mfa:EncryptionKey"] = "AABBCCDDEEFF00112233445566778899AABBCCDDEEFF00112233445566778899",
                ["Internal:ApiKey"] = "test-internal-key",
            });
        });

        builder.ConfigureServices(services =>
        {
            // EF Core registers the Npgsql provider via composable IDbContextOptionsConfiguration<T>
            // entries, not just DbContextOptions<T> — removing only the latter leaves the Npgsql
            // configuration in place, so both providers end up registered once InMemory is added.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));

            // Each factory instance gets its own isolated database.
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase("Sprint6And7TestDb_" + Guid.NewGuid()));
        });
    }

    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    /// <summary>
    /// Seeds a user with the given role, logs in via POST /auth/login,
    /// and returns the client with the session cookie attached.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        var client = CreateAnonymousClient();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var username = $"test-{role.ToLower()}-{Guid.NewGuid().ToString("N")[..8]}";
        const string password = "TestPassword123!";

        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = hasher.Hash(password),
            Role = Enum.Parse<UserRole>(role, ignoreCase: true),
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { username, password });
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }
}
