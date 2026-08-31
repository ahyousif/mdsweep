using System.Globalization;
using Mdsweep.Application.TripImports;

namespace Mdsweep.Infrastructure.TripImports.Parsing;

internal static class TripImportTabularRows
{
    private static readonly string[] AppointmentDateFormats = ["M/d/yyyy", "MM/dd/yyyy", "M/d/yy", "yyyy-MM-dd"];
    private static readonly string[] AppointmentTimeFormats = ["H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss", "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt"];
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
            var date = Cell(row, "Appointment Date")?.Trim();
            var time = Cell(row, "Time")?.Trim();
            var hasAppointmentDate = !string.IsNullOrWhiteSpace(date);
            var hasAppointmentTime = !string.IsNullOrWhiteSpace(time);
            var hasValidAppointmentDate = DateOnly.TryParseExact(
                date, AppointmentDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var serviceDate
            );
            var hasValidAppointmentTime = TimeOnly.TryParseExact(
                time, AppointmentTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var appointmentTime
            );
            return new ParsedTripImportRow(
                offset + 2, Cell(row, "Trip Number"), Cell(row, "Medicaid Number"),
                Cell(row, "Member's First Name"), Cell(row, "Member's Last Name"),
                hasValidAppointmentDate ? serviceDate : null,
                hasValidAppointmentTime ? LocalTime.FromTimeOnly(appointmentTime) : null,
                Cell(row, "Pickup Address"), Cell(row, "Pickup City"), Cell(row, "Delivery Address"),
                Cell(row, "Delivery City"), Cell(row, "Trip Status"),
                string.Equals(Cell(row, "Will Call Flag"), "Y", StringComparison.OrdinalIgnoreCase),
                hasAppointmentDate && !hasValidAppointmentDate ? $"Appointment Date '{date}' is invalid." : null,
                hasAppointmentTime && !hasValidAppointmentTime ? $"Appointment Time '{time}' is invalid." : null
            );
        }).ToList();
    }
}
