using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MMDb.Helpers;

/// <summary>
/// Simplifies text for torrent searches, which match plain release names better than punctuated titles.
/// </summary>
public static partial class SearchText
{
    /// <summary>
    /// Strips accents, replaces punctuation with spaces and collapses repeated spaces, e.g. "Amélie: Part 2" becomes "Amelie Part 2".
    /// </summary>
    /// <param name="text">The text to simplify.</param>
    public static string Simplify(string text)
    {
        StringBuilder builder = new();
        foreach (char character in text.Normalize(NormalizationForm.FormD))
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }
        return RepeatedSpaces().Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ").Trim();
    }

    /// <summary>
    /// Matches runs of whitespace.
    /// </summary>
    [GeneratedRegex(@"\s+")]
    private static partial Regex RepeatedSpaces();
}