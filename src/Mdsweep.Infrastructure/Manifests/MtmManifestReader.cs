using Mdsweep.Application.Trips.Import.Manifest;

namespace Mdsweep.Infrastructure.Manifests;

internal sealed class MtmManifestReader : IMtmManifestReader
{
    public Task<MtmManifestReadResult> ReadAsync(string fileName, ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        var table = Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".csv" => ReadCsv(content),
            ".xlsx" => ReadExcel(content),
            _ => throw new NotSupportedException($"The file type '{Path.GetExtension(fileName)}' is not supported."),
        };

        return Task.FromResult(MtmManifestParser.Parse(table));
    }

    private static List<IReadOnlyList<string>> ReadCsv(ReadOnlyMemory<byte> content)
    {
        using var stream = new MemoryStream(content.ToArray());
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        using var csv = new CsvReader(
            reader,
            new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null }
        );

        if (!csv.Read() || !csv.ReadHeader())
        {
            return [];
        }

        var headers = csv.HeaderRecord ?? [];

        var rows = new List<IReadOnlyList<string>> { headers };

        while (csv.Read())
        {
            rows.Add([.. Enumerable.Range(0, headers.Length).Select(index => csv.GetField(index) ?? string.Empty)]);
        }

        return rows;
    }

    private static List<IReadOnlyList<string>> ReadExcel(ReadOnlyMemory<byte> content)
    {
        using var stream = new MemoryStream(content.ToArray());
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            return [];
        }

        var firstRow = worksheet.FirstRowUsed();

        if (firstRow is null)
        {
            return [];
        }

        var lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        if (lastColumn == 0)
        {
            return [];
        }

        return worksheet
            .RowsUsed()
            .Select(row =>
                (IReadOnlyList<string>)
                    [
                        .. Enumerable
                            .Range(1, lastColumn)
                            .Select(column => worksheet.Cell(row.RowNumber(), column).GetFormattedString()),
                    ]
            )
            .ToList();
    }
}
