using Mdsweep.Application.TripImports;

namespace Mdsweep.Infrastructure.TripImports.Parsing;

internal static class TripImportTabularRows
{
    private static readonly string[] RequiredHeaders =
    [
        "Trip Number", "Medicaid Number", "Appointment Date", "Member's First Name", "Member's Last Name"
    ];

    public static IReadOnlyList<ParsedTripImportRow> Read(IReadOnlyList<IReadOnlyList<string>> table)
    {
        if (table.Count == 0) throw new TripImportParseException("The import file is empty.");
        var headers = table[0]
            .Select((value, index) => new { value, index })
            .ToDictionary(cell => cell.value, cell => cell.index, StringComparer.OrdinalIgnoreCase);
        foreach (var header in RequiredHeaders)
            if (!headers.ContainsKey(header)) throw new TripImportParseException($"The import file is missing the '{header}' column.");

        string? Cell(IReadOnlyList<string> row, string header) =>
            headers.TryGetValue(header, out var index) && index < row.Count ? row[index] : null;
        return table.Skip(1).Select((row, offset) =>
        {
            var date = Cell(row, "Appointment Date");
            var time = Cell(row, "Time");
            return new ParsedTripImportRow(
                offset + 2, Cell(row, "Trip Number"), Cell(row, "Medicaid Number"),
                Cell(row, "Member's First Name"), Cell(row, "Member's Last Name"),
                DateOnly.TryParse(date, out var serviceDate) ? serviceDate : null,
                TimeOnly.TryParse(time, out var appointmentTime) ? LocalTime.FromTimeOnly(appointmentTime) : null,
                Cell(row, "Pickup Address"), Cell(row, "Pickup City"), Cell(row, "Delivery Address"),
                Cell(row, "Delivery City"), Cell(row, "Trip Status"),
                string.Equals(Cell(row, "Will Call Flag"), "Y", StringComparison.OrdinalIgnoreCase)
            );
        }).ToList();
    }
}
