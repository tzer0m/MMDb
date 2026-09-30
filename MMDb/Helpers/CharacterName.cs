using System.Text.RegularExpressions;

namespace MMDb.Helpers;

/// <summary>
/// Tidies character names from TMDb.
/// </summary>
public static partial class CharacterName
{
    /// <summary>
    /// Removes "(voice)" from a character name, returning null if nothing is left.
    /// </summary>
    /// <param name="character">The character name from TMDb.</param>
    public static string? Clean(string? character)
    {
        string cleaned = VoiceSuffix().Replace(character ?? string.Empty, string.Empty).Trim();
        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>
    /// Matches "(voice)" and any whitespace before it, ignoring case.
    /// </summary>
    [GeneratedRegex(@"\s*\(voice\)", RegexOptions.IgnoreCase)]
    private static partial Regex VoiceSuffix();
}