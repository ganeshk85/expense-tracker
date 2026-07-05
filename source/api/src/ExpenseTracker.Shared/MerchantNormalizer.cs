using System.Text.RegularExpressions;

namespace ExpenseTracker.Shared;

/// <summary>
/// Normalizes merchant names to a canonical lowercase form used for
/// category mapping, duplicate detection, and tag history lookups.
/// The algorithm must produce identical output to the Python OCR worker's
/// normalize_merchant() function — verified by shared test fixtures.
/// </summary>
public static partial class MerchantNormalizer
{
    // Remove punctuation characters: . , ' " - _ & /
    [GeneratedRegex(@"[.,'""\-_&/]", RegexOptions.Compiled)]
    private static partial Regex PunctuationPattern();

    // Collapse multiple whitespace characters into a single space
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();

    /// <summary>
    /// Normalizes a merchant name: lowercase, strip punctuation, collapse whitespace.
    /// Returns an empty string for null or whitespace-only input.
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var result = name.ToLowerInvariant();
        result = PunctuationPattern().Replace(result, " ");
        result = WhitespacePattern().Replace(result, " ");
        return result.Trim();
    }
}
