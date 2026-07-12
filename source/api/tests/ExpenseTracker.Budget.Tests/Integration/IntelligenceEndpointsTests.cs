using System.Net;
using System.Net.Http.Json;

namespace ExpenseTracker.Budget.Tests.Integration;

/// <summary>
/// Integration tests for Sprint 8 (Phase 3: Auto-Categorization + Duplicate Detection)
/// intelligence endpoints and the duplicate-detection flow on expense creation.
/// Reuses Sprint6And7WebAppFactory — an in-memory database with real session
/// authentication via POST /auth/login.
/// </summary>

// ── Merchant category map (Owner/Admin-only transparency endpoint) ──────────────

public sealed class MerchantMapEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public MerchantMapEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMerchantMap_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/intelligence/merchant-map");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMerchantMap_AsContributor_Returns403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/intelligence/merchant-map");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMerchantMap_AsAdmin_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/intelligence/merchant-map");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MerchantMapBody>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    private sealed record MerchantMapBody(IReadOnlyList<object> Items);
}

// ── OCR accuracy (Owner/Admin-only) ──────────────────────────────────────────────

public sealed class OcrAccuracyEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public OcrAccuracyEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetOcrAccuracy_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/intelligence/ocr-accuracy");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOcrAccuracy_AsContributor_Returns403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/intelligence/ocr-accuracy");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOcrAccuracy_AsAdmin_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Admin");
        var response = await client.GetAsync("/intelligence/ocr-accuracy");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OcrAccuracyBody>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    private sealed record OcrAccuracyBody(IReadOnlyList<object> Items);
}

// ── Tag suggestions ───────────────────────────────────────────────────────────

public sealed class TagSuggestionsEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public TagSuggestionsEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetTagSuggestions_WithoutSession_Returns401()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/intelligence/tag-suggestions?merchant=Woolworths");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTagSuggestions_WithSession_Returns200WithEmptyListWhenNoHistory()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var response = await client.GetAsync("/intelligence/tag-suggestions?merchant=Unknown+Merchant");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TagSuggestionsBody>();
        body.Should().NotBeNull();
        body!.Tags.Should().BeEmpty();
    }

    private sealed record TagSuggestionsBody(IReadOnlyList<string> Tags);
}

// ── Duplicate detection on expense create/get/dismiss (US-INT-02) ───────────────

public sealed class DuplicateDetectionEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public DuplicateDetectionEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateExpense_ExactMatchOnMerchantAmountDate_ReturnsHighConfidenceDuplicateWarning()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var date = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        var first = await client.PostAsJsonAsync("/expenses", new
        {
            merchantName = "Woolworths",
            date,
            total = 42.50m,
            category = "Groceries",
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/expenses", new
        {
            merchantName = "Woolworths",
            date,
            total = 42.50m,
            category = "Groceries",
        });
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await second.Content.ReadFromJsonAsync<ExpenseWithWarningBody>();
        body.Should().NotBeNull();
        body!.DuplicateWarning.Should().NotBeNull();
        body.DuplicateWarning!.Confidence.Should().Be("high");
    }

    [Fact]
    public async Task CreateExpense_SameMerchantAndAmountOneDayApart_ReturnsPossibleConfidence()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");

        var first = await client.PostAsJsonAsync("/expenses", new
        {
            merchantName = "Coles",
            date = new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero),
            total = 19.99m,
            category = "Groceries",
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/expenses", new
        {
            merchantName = "Coles",
            date = new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero),
            total = 19.99m,
            category = "Groceries",
        });
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await second.Content.ReadFromJsonAsync<ExpenseWithWarningBody>();
        body!.DuplicateWarning!.Confidence.Should().Be("possible");
    }

    [Fact]
    public async Task DismissDuplicate_SuppressesWarningOnSubsequentGet()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");
        var date = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

        var first = await client.PostAsJsonAsync("/expenses", new
        {
            merchantName = "Aldi",
            date,
            total = 15.00m,
            category = "Groceries",
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/expenses", new
        {
            merchantName = "Aldi",
            date,
            total = 15.00m,
            category = "Groceries",
        });
        var secondBody = await second.Content.ReadFromJsonAsync<ExpenseWithWarningBody>();
        secondBody!.DuplicateWarning.Should().NotBeNull();

        var dismiss = await client.PostAsync($"/expenses/{secondBody.Id}/dismiss-duplicate", null);
        dismiss.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reload = await client.GetAsync($"/expenses/{secondBody.Id}");
        var reloadBody = await reload.Content.ReadFromJsonAsync<ExpenseWithWarningBody>();
        reloadBody!.DuplicateWarning.Should().BeNull();
    }

    private sealed record ExpenseWithWarningBody(Guid Id, DuplicateWarningBody? DuplicateWarning);

    private sealed record DuplicateWarningBody(Guid ExistingExpenseId, DateTimeOffset? ExistingDate, string Confidence);
}

// ── Category suggestion on repeated confirmed merchant (US-INT-01) ───────────────

public sealed class CategorySuggestionEndpointsTests : IClassFixture<Sprint6And7WebAppFactory>
{
    private readonly Sprint6And7WebAppFactory _factory;
    public CategorySuggestionEndpointsTests(Sprint6And7WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetExpense_NoConfirmedHistoryForMerchant_OmitsSuggestion()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Contributor");

        var created = await client.PostAsJsonAsync("/expenses", new
        {
            merchantName = "Brand New Merchant Inc",
            date = new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero),
            total = 10.00m,
            category = "Other",
        });
        var body = await created.Content.ReadFromJsonAsync<ExpenseSuggestionBody>();

        var get = await client.GetAsync($"/expenses/{body!.Id}");
        var getBody = await get.Content.ReadFromJsonAsync<ExpenseSuggestionBody>();
        getBody!.SuggestedCategory.Should().BeNull();
    }

    private sealed record ExpenseSuggestionBody(Guid Id, string? SuggestedCategory, string? SuggestionConfidence);
}
