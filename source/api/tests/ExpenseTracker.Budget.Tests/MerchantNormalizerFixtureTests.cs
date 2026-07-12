using System.Runtime.CompilerServices;
using System.Text.Json;
using ExpenseTracker.Shared;

namespace ExpenseTracker.Budget.Tests;

/// <summary>
/// Verifies MerchantNormalizer.Normalize produces output identical to the Python
/// OCR worker's normalize_merchant() for every case in the shared fixture file,
/// per Sprint 8 US-INT-01 (normalization must match exactly between .NET and Python).
/// </summary>
public sealed class MerchantNormalizerFixtureTests
{
    public static IEnumerable<object[]> FixtureCases()
    {
        var path = ResolveFixturePath();
        var json = File.ReadAllText(path);
        var cases = JsonSerializer.Deserialize<List<FixtureCase>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? [];

        foreach (var c in cases)
            yield return [c.Input, c.Expected];
    }

    [Theory]
    [MemberData(nameof(FixtureCases))]
    public void Normalize_MatchesSharedFixture(string input, string expected)
    {
        MerchantNormalizer.Normalize(input).Should().Be(expected);
    }

    private static string ResolveFixturePath([CallerFilePath] string callerPath = "")
    {
        var dir = Path.GetDirectoryName(callerPath)!;
        var path = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "ocr", "tests", "merchant_normalization_fixtures.json"));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Shared merchant normalization fixture not found at '{path}'.");
        return path;
    }

    private sealed record FixtureCase(string Input, string Expected);
}
