using ExpenseTracker.Expense.Entities;
using ExpenseTracker.Expense.Repositories;
using ExpenseTracker.Expense.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExpenseTracker.Budget.Tests.Services;

/// <summary>
/// Unit tests for the Sprint 8 US-INT-01 category-suggestion confidence thresholds.
/// The integration-level test (CategorySuggestionEndpointsTests) can only exercise the
/// "no history" path via the public API, since OCR-sourced ConfidenceJson is not
/// settable through a public endpoint — this test covers the confirmed_count thresholds directly.
/// </summary>
public sealed class IntelligenceServiceTests
{
    private static readonly Guid HouseholdId = Guid.NewGuid();

    private static IntelligenceService CreateService(MerchantCategoryMap? entry)
    {
        var repo = new Mock<IIntelligenceRepository>();
        repo.Setup(r => r.FindMerchantCategoryAsync(HouseholdId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        return new IntelligenceService(repo.Object, NullLogger<IntelligenceService>.Instance);
    }

    [Fact]
    public async Task GetSuggestedCategoryAsync_NoMerchantEntry_ReturnsNull()
    {
        var service = CreateService(entry: null);

        var result = await service.GetSuggestedCategoryAsync(HouseholdId, "Woolworths");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetSuggestedCategoryAsync_BelowThreeConfirmations_ReturnsLowConfidence(int confirmedCount)
    {
        var entry = new MerchantCategoryMap
        {
            HouseholdId = HouseholdId,
            MerchantNameNormalized = "woolworths",
            Category = "Groceries",
            ConfirmedCount = confirmedCount,
            LastConfirmedAt = DateTimeOffset.UtcNow,
        };
        var service = CreateService(entry);

        var result = await service.GetSuggestedCategoryAsync(HouseholdId, "Woolworths");

        result.Should().NotBeNull();
        result!.Category.Should().Be("Groceries");
        result.Confidence.Should().Be("low");
    }

    [Fact]
    public async Task GetSuggestedCategoryAsync_ThreeOrMoreConfirmations_ReturnsHighConfidence()
    {
        var entry = new MerchantCategoryMap
        {
            HouseholdId = HouseholdId,
            MerchantNameNormalized = "woolworths",
            Category = "Groceries",
            ConfirmedCount = 3,
            LastConfirmedAt = DateTimeOffset.UtcNow,
        };
        var service = CreateService(entry);

        var result = await service.GetSuggestedCategoryAsync(HouseholdId, "Woolworths");

        result.Should().NotBeNull();
        result!.Confidence.Should().Be("high");
    }
}
