namespace MMDb.Options;

/// <summary>
/// Configuration for signing in with Pocket ID over OpenID Connect.
/// </summary>
public class OidcOptions
{
    /// <summary>
    /// The Pocket ID server URL.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The OIDC client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The OIDC client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}