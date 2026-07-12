using System.Net;
using System.Net.Http.Json;

namespace ExpenseTracker.Budget.Tests.Integration;

/// <summary>
/// Integration tests for Sprint 9 (Phase 3: Merchant Template Learning + Recurring
/// Expense Detection) endpoints. Reuses Sprint6And7WebAppFactory — an in-memory
/// database with real session authentication via POST /auth/login.
/// </summary>

// ── Merchant field templates (US-INT-05) ─────────────────────────────────────

public sealed class MerchantTemplatesEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public MerchantTemplatesEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMerchantTemplates_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/intelligence/merchant-templates");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMerchantTemplates_AsContributor_Returns403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/intelligence/merchant-templates");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMerchantTemplates_AsAdmin_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/intelligence/merchant-templates");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TemplatesBody>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteMerchantTemplates_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.DeleteAsync("/intelligence/merchant-templates/woolworths");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMerchantTemplates_AsAdmin_NoExistingTemplate_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.DeleteAsync("/intelligence/merchant-templates/no-such-merchant");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostInternalMerchantTemplate_WithoutInternalKey_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/internal/merchant-templates", new
        {
            merchantName = "Woolworths",
            fieldName = "total",
            regionX = 0.1,
            regionY = 0.2,
            regionW = 0.3,
            regionH = 0.05,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostInternalMerchantTemplate_WithInternalKey_Returns204()
    {
        var client = _factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", "test-internal-key");

        var response = await client.PostAsJsonAsync("/internal/merchant-templates", new
        {
            merchantName = "Woolworths",
            fieldName = "total",
            regionX = 0.1,
            regionY = 0.2,
            regionW = 0.3,
            regionH = 0.05,
        });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record TemplatesBody(IReadOnlyList<object> Items);
}

// ── Recurring expenses (US-INT-06) ───────────────────────────────────────────

public sealed class RecurringExpensesEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public RecurringExpensesEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetRecurring_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/intelligence/recurring");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRecurring_WithSession_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/intelligence/recurring");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RecurringBody>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SnoozeRecurring_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.PostAsync($"/intelligence/recurring/{Guid.NewGuid()}/snooze?days=30", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SnoozeRecurring_UnknownId_StillReturns204NoContent()
    {
        // Snooze is a no-op for an unknown id (matches the existing dismiss-duplicate
        // pattern in this codebase — idempotent, doesn't leak existence via 404).
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.PostAsync($"/intelligence/recurring/{Guid.NewGuid()}/snooze?days=30", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record RecurringBody(IReadOnlyList<object> Items);
}

// ── Merchant aliases (US-INT-07) ──────────────────────────────────────────────

public sealed class MerchantAliasesEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public MerchantAliasesEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAliases_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/intelligence/merchant-aliases");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAlias_AsContributor_Returns403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.PostAsJsonAsync("/intelligence/merchant-aliases", new
        {
            alias = "Woolworths 42",
            canonical = "Woolworths",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAlias_AsAdmin_ThenListsIt()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");

        var create = await client.PostAsJsonAsync("/intelligence/merchant-aliases", new
        {
            alias = "Woolworths 18",
            canonical = "Woolworths",
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await client.GetAsync("/intelligence/merchant-aliases");
        var body = await list.Content.ReadFromJsonAsync<AliasesBody>();
        body!.Items.Should().Contain(i => i.AliasNormalized == "woolworths 18" && i.CanonicalNormalized == "woolworths");
    }

    [Fact]
    public async Task CreateAlias_SameAliasAndCanonical_Returns422()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.PostAsJsonAsync("/intelligence/merchant-aliases", new
        {
            alias = "Woolworths",
            canonical = "Woolworths",
        });
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record AliasesBody(IReadOnlyList<AliasEntry> Items);
    private sealed record AliasEntry(Guid Id, string AliasNormalized, string CanonicalNormalized, DateTimeOffset CreatedAt);
}

// ── Intelligence summary (US-INT-08) ──────────────────────────────────────────

public sealed class IntelligenceSummaryEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public IntelligenceSummaryEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetSummary_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/intelligence/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_AsContributor_Returns403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/intelligence/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSummary_AsAdmin_Returns200WithCounts()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/intelligence/summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SummaryBody>();
        body.Should().NotBeNull();
    }

    private sealed record SummaryBody(int MerchantMappings, int FieldTemplates, int RecurringExpenses, int Aliases);
}
