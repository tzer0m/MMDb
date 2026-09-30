namespace MMDb.Helpers;

/// <summary>
/// Picks colours for a rating, from red at the bottom of the scale through amber to dark green at the top.
/// </summary>
public static class RatingColour
{
    /// <summary>
    /// Returns the background hex colour for a rating.
    /// </summary>
    /// <param name="rating">The rating.</param>
    /// <param name="min">The lowest possible rating.</param>
    /// <param name="max">The highest possible rating.</param>
    public static string ForRating(double rating, double min = 1, double max = 10)
    {
        (int red, int green, int blue) = Blend(rating, min, max);
        return $"#{red:x2}{green:x2}{blue:x2}";
    }

    /// <summary>
    /// Returns an inline style that sets the background to the rating's colour.
    /// </summary>
    /// <param name="rating">The rating.</param>
    /// <param name="min">The lowest possible rating.</param>
    /// <param name="max">The highest possible rating.</param>
    public static string StyleForRating(double rating, double min = 1, double max = 10)
    {
        return $"background-color: {ForRating(rating, min, max)}";
    }

    /// <summary>
    /// Blends between red, amber and dark green according to where the rating sits on the scale.
    /// </summary>
    /// <param name="rating">The rating.</param>
    /// <param name="min">The lowest possible rating.</param>
    /// <param name="max">The highest possible rating.</param>
    private static (int Red, int Green, int Blue) Blend(double rating, double min, double max)
    {
        (int Red, int Green, int Blue)[] stops = [(0xff, 0x00, 0x00), (0xff, 0xc0, 0x00), (0x00, 0x80, 0x00)];
        double position = Math.Clamp((rating - min) / (max - min), 0, 1) * (stops.Length - 1);
        int index = Math.Min((int)position, stops.Length - 2);
        double fraction = position - index;
        (int Red, int Green, int Blue) value = stops[index];
        (int Red, int Green, int Blue) from = value;
        (int Red, int Green, int Blue) = stops[index + 1];
        return (Mix(from.Red, Red, fraction), Mix(from.Green, Green, fraction), Mix(from.Blue, Blue, fraction));
    }

    /// <summary>
    /// Linearly mixes two colour channel values.
    /// </summary>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="fraction">How far to move from the start towards the end, from 0 to 1.</param>
    private static int Mix(int from, int to, double fraction)
    {
        return (int)Math.Round(from + ((to - from) * fraction));
    }
}