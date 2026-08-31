using System.Globalization;
using Mdsweep.Domain.ManifestImports;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class ManifestXlsx
{
    public static IReadOnlyList<ManifestReceiptRow> Preview(Stream source)
    {
        try
        {
            using var workbook = new XLWorkbook(source);
            var worksheet =
                workbook.Worksheets.FirstOrDefault(HasContent)
                ?? throw new ManifestFormatException("The workbook is empty.");
            var range =
                worksheet.RangeUsed()
                ?? throw new ManifestFormatException("The workbook is empty.");
            var rows = range.RowsUsed().ToArray();
            var headers = rows[0]
                .Cells(1, range.ColumnCount())
                .Select(cell => cell.GetString().Trim())
                .ToArray();
            var records = new List<IReadOnlyList<string>> { headers };

            foreach (var row in rows.Skip(1))
            {
                records.Add(
                    row.Cells(1, headers.Length)
                        .Select((cell, index) => CellValue(cell, headers[index]))
                        .ToArray()
                );
            }

            return ManifestTabular.Preview(records);
        }
        catch (ManifestFormatException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ManifestFormatException(
                "The Excel workbook could not be read. Upload a valid .xlsx file."
            );
        }
    }

    private static bool HasContent(IXLWorksheet worksheet) => worksheet.RangeUsed() is not null;

    private static string CellValue(IXLCell cell, string header)
    {
        if (
            header.Equals("Appointment Date", StringComparison.OrdinalIgnoreCase)
            && cell.TryGetValue<DateTime>(out var date)
        )
            return date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);

        if (header.Equals("Time", StringComparison.OrdinalIgnoreCase))
        {
            if (cell.TryGetValue<TimeSpan>(out var time))
                return $"{time.Hours:00}{time.Minutes:00}";
            if (cell.TryGetValue<DateTime>(out var dateTime))
                return dateTime.ToString("HHmm", CultureInfo.InvariantCulture);
            if (cell.TryGetValue<double>(out var number) && number >= 0 && number < 1)
            {
                var excelTime = TimeSpan.FromDays(number);
                return $"{excelTime.Hours:00}{excelTime.Minutes:00}";
            }
        }

        return cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();
    }
}
