namespace MMDb.Options;

/// <summary>
/// Configuration for the OMDb API.
/// </summary>
public class OMDbOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "OMDb";

    /// <summary>
    /// The OMDb API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}