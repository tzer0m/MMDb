using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MMDb.Helpers;
using MMDb.Models;
using MMDb.Models.TMDb;
using MMDb.Options;
using MMDb.Services;

namespace MMDb.Pages.Shows;

/// <summary>
/// Shows a TV show from TMDb with its main cast and my rated films each of them is in.
/// </summary>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="filmPeople">The film people service.</param>
/// <param name="options">The people options.</param>
/// <param name="logger">The logger.</param>
public partial class DetailsModel(TMDbClient tmdb, FilmPeopleService filmPeople, IOptions<PeopleOptions> options, ILogger<DetailsModel> logger) : PageModel
{
    /// <summary>
    /// The show being displayed.
    /// </summary>
    public TMDbTvShow Show { get; set; } = null!;

    /// <summary>
    /// The show's main cast, with my rated films each of them is in.
    /// </summary>
    public FilmPeople People { get; set; } = new();

    /// <summary>
    /// How many seasons and episodes the show has, e.g. 5 seasons · 62 episodes.
    /// </summary>
    public string? EpisodesText
    {
        get
        {
            List<string> parts = [];
            if (Show.NumberOfSeasons > 0)
            {
                parts.Add(Show.NumberOfSeasons == 1 ? "1 season" : $"{Show.NumberOfSeasons} seasons");
            }
            if (Show.NumberOfEpisodes > 0)
            {
                parts.Add(Show.NumberOfEpisodes == 1 ? "1 episode" : $"{Show.NumberOfEpisodes} episodes");
            }
            return parts.Count == 0 ? null : string.Join(" \u00b7 ", parts);
        }
    }

    /// <summary>
    /// Loads the show and its cast from TMDb; signed out, show pages are not available.
    /// </summary>
    /// <param name="tmdbId">The TMDb TV show ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(int tmdbId, CancellationToken cancellationToken)
    {
        // Show pages call TMDb on every visit, so signed out they 404; otherwise crawlers can walk every show on TMDb.
        if (User.Identity?.IsAuthenticated != true)
        {
            return NotFound();
        }
        TMDbTvShow? show;
        try
        {
            show = await tmdb.GetTvShowAsync(tmdbId, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogShowFailed(logger, ex, tmdbId);
            return NotFound();
        }
        if (show is null)
        {
            return NotFound();
        }
        Show = show;
        List<FilmCredit> credits = [.. (show.AggregateCredits?.Cast ?? []).OrderBy(x => x.Order).Take(options.Value.CastCount).Select(x => new FilmCredit { PersonId = x.Id, Person = new Person { PersonId = x.Id, Name = x.Name, ProfilePath = x.ProfilePath }, Role = CreditRole.Cast, Order = x.Order, Character = DescribeCharacters(x) })];
        People = await filmPeople.LoadAsync(credits, 0, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Describes the characters a cast member played, most episodes first, or null if none are named.
    /// </summary>
    /// <param name="member">The cast member.</param>
    private static string? DescribeCharacters(TMDbAggregateCastMember member)
    {
        string characters = string.Join(", ", member.Roles.OrderByDescending(x => x.EpisodeCount).Select(x => CharacterName.Clean(x.Character)).OfType<string>().Distinct());
        return characters.Length == 0 ? null : characters;
    }

    /// <summary>
    /// Logs that a show's details could not be fetched from TMDb.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="tmdbId">The TMDb TV show ID.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not fetch TMDb details for TV show {TMDbId}.")]
    private static partial void LogShowFailed(ILogger logger, Exception exception, int tmdbId);
}