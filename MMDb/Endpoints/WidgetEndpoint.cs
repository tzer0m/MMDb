using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Endpoints;

/// <summary>
/// The endpoint that feeds the Tidgets home screen widget. It needs no API key because the same films are already public on the home page.
/// </summary>
public static class WidgetEndpoint
{
    /// <summary>
    /// Returns the number of films watched as JSON.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<IResult> GetAsync(MMDbContext db, CancellationToken cancellationToken)
    {
        return Results.Ok(new WidgetResponse { Films = await db.Films.CountAsync(cancellationToken) });
    }
}