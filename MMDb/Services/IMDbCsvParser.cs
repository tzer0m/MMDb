using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using MMDb.Models.Import;

namespace MMDb.Services;

// ONE-TIME IMPORT: remove this class once the Jellyfin/IMDb import is complete.
/// <summary>
/// Parses an IMDb ratings CSV export.
/// </summary>
public static class IMDbCsvParser
{
    /// <summary>
    /// Reads every rating from the export, locating columns by their header names.
    /// </summary>
    /// <param name="stream">The CSV file contents.</param>
    public static List<IMDbRatingRow> Parse(Stream stream)
    {
        List<IMDbRatingRow> rows = [];
        using TextFieldParser parser = new(stream, Encoding.UTF8);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        string[] headers = parser.ReadFields() ?? throw new InvalidDataException("The CSV file is empty.");
        int idColumn = FindColumn(headers, "Const");
        int ratingColumn = FindColumn(headers, "Your Rating");
        int dateColumn = FindColumn(headers, "Date Rated");
        int titleColumn = FindColumn(headers, "Title");
        int yearColumn = FindColumn(headers, "Year");
        int typeColumn = FindColumn(headers, "Title Type");
        while (!parser.EndOfData)
        {
            string[]? fields = parser.ReadFields();
            if (fields is null || fields.Length < headers.Length || !int.TryParse(fields[ratingColumn], out int rating))
            {
                continue;
            }
            IMDbRatingRow row = new()
            {
                IMDbId = fields[idColumn].Trim(),
                Rating = rating,
                Title = fields[titleColumn].Trim(),
                TitleType = fields[typeColumn].Trim(),
                DateRated = DateOnly.TryParseExact(fields[dateColumn], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dateRated) ? dateRated : null,
                Year = int.TryParse(fields[yearColumn], out int year) ? year : null
            };
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>
    /// Finds the index of a column by its header name.
    /// </summary>
    /// <param name="headers">The header row.</param>
    /// <param name="name">The column name to find.</param>
    private static int FindColumn(string[] headers, string name)
    {
        int index = Array.FindIndex(headers, x => x.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : throw new InvalidDataException($"The CSV file has no '{name}' column. Is it an IMDb ratings export?");
    }
}