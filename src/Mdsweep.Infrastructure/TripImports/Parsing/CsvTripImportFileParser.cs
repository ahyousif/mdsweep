using Mdsweep.Application.TripImports;
using Mdsweep.Application.TripImports.Abstractions;

namespace Mdsweep.Infrastructure.TripImports.Parsing;

public sealed class CsvTripImportFileParser : ITripImportFileParser
{
    public bool CanParse(string fileName, string? contentType) =>
        fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<ParsedTripImportRow>> ParseAsync(ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        using var reader = new StringReader(Encoding.UTF8.GetString(content.Span));
        var lines = new List<IReadOnlyList<string>>();
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
            lines.Add(ReadCsvLine(line));
        return TripImportTabularRows.Read(lines);
    }

    private static IReadOnlyList<string> ReadCsvLine(string line)
    {
        var cells = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"' && quoted && index + 1 < line.Length && line[index + 1] == '"')
            {
                value.Append(character);
                index++;
            }
            else if (character == '"') quoted = !quoted;
            else if (character == ',' && !quoted)
            {
                cells.Add(value.ToString());
                value.Clear();
            }
            else value.Append(character);
        }
        if (quoted) throw new TripImportParseException("The CSV file contains an unterminated quoted value.");
        cells.Add(value.ToString());
        return cells;
    }
}
