using System.Globalization;
using Mdsweep.Application.TripImports;
using Mdsweep.Application.TripImports.Abstractions;

namespace Mdsweep.Infrastructure.TripImports.Parsing;

public sealed class CsvTripImportFileParser : ITripImportFileParser
{
    public bool CanParse(string fileName, string? contentType) => fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ParsedTripImportRow>> ParseAsync(ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(new MemoryStream(content.ToArray()), detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null });
            if (!csv.Read() || !csv.ReadHeader()) throw new TripImportParseException("The import file is empty.");
            var headers = csv.HeaderRecord ?? [];
            var rows = new List<IReadOnlyList<string>> { headers };
            while (csv.Read()) rows.Add(headers.Select(header => csv.GetField(header) ?? string.Empty).ToList());
            return Task.FromResult(TripImportTabularRows.Read(rows));
        }
        catch (TripImportParseException) { throw; }
        catch (Exception) { throw new TripImportParseException("The CSV file could not be read."); }
    }
}
