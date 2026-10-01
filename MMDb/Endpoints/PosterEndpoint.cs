using System.Text.RegularExpressions;
using MMDb.Services;

namespace MMDb.Endpoints;

/// <summary>
/// Serves my Jellyfin posters, so browsers never talk to Jellyfin directly.
/// </summary>
public static partial class PosterEndpoint
{
    /// <summary>
    /// Returns a film's Jellyfin primary image, cached by browsers for a year since the tag in the URL changes whenever the poster does.
    /// </summary>
    /// <param name="itemId">The Jellyfin item ID.</param>
    /// <param name="tag">The Jellyfin image tag.</param>
    /// <param name="w">The width in pixels.</param>
    /// <param name="jellyfin">The Jellyfin client.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<IResult> GetAsync(string itemId, string tag, int? w, JellyfinClient jellyfin, HttpContext context, CancellationToken cancellationToken)
    {
        if (!HexId().IsMatch(itemId) || !HexId().IsMatch(tag))
        {
            return Results.NotFound();
        }
        (byte[] Content, string ContentType)? image;
        try
        {
            image = await jellyfin.GetPrimaryImageAsync(itemId, tag, Math.Clamp(w ?? 342, 50, 1000), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
        if (image is null)
        {
            return Results.NotFound();
        }
        context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Results.File(image.Value.Content, image.Value.ContentType);
    }

    /// <summary>
    /// Matches a Jellyfin item ID or image tag.
    /// </summary>
    [GeneratedRegex("^[0-9a-fA-F]{1,64}$")]
    private static partial Regex HexId();
}