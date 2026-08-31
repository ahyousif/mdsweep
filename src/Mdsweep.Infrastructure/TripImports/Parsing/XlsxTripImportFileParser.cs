using Mdsweep.Application.TripImports;
using Mdsweep.Application.TripImports.Abstractions;

namespace Mdsweep.Infrastructure.TripImports.Parsing;

public sealed class XlsxTripImportFileParser : ITripImportFileParser
{
    public bool CanParse(string fileName, string? contentType) =>
        fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ParsedTripImportRow>> ParseAsync(ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        try
        {
            using var stream = new MemoryStream(content.ToArray());
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new TripImportParseException("The XLSX file does not contain a worksheet.");
            var rows = worksheet.RowsUsed()
                .Select(row => (IReadOnlyList<string>)row.CellsUsed().Select(cell => cell.GetFormattedString()).ToList())
                .ToList();
            return Task.FromResult(TripImportTabularRows.Read(rows));
        }
        catch (TripImportParseException) { throw; }
        catch (Exception) { throw new TripImportParseException("The XLSX file could not be read."); }
    }
}
