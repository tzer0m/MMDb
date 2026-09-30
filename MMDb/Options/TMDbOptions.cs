namespace MMDb.Options;

/// <summary>
/// Configuration for the TMDb API.
/// </summary>
public class TMDbOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "TMDb";

    /// <summary>
    /// The TMDb API read access token.
    /// </summary>
    public string ApiReadAccessToken { get; set; } = string.Empty;
}