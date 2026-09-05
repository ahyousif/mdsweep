using System.Globalization;
using Mdsweep.Application.TripImports;

namespace Mdsweep.Infrastructure.TripImports.Parsing;

internal static class TripImportTabularRows
{
    private static readonly string[] AppointmentDateFormats = ["M/d/yyyy", "MM/dd/yyyy", "M/d/yy", "yyyy-MM-dd"];
    private static readonly string[] AppointmentTimeFormats = ["HHmm", "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss", "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt"];
    private static readonly string[] RequiredHeaders =
    [
        "Trip Number", "Medicaid Number", "Appointment Date", "Member's First Name", "Member's Last Name"
    ];

    public static IReadOnlyList<ParsedTripImportItem> Read(IReadOnlyList<IReadOnlyList<string>> table)
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
            var cost = Cell(row, "Trip Cost")?.Trim();
            var mileage = Cell(row, "Trip Mileage")?.Trim();
            var hasValidCost = decimal.TryParse(cost, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out var tripCost);
            var hasValidMileage = decimal.TryParse(mileage, NumberStyles.Number, CultureInfo.InvariantCulture, out var tripMileage);
            var rawPassengerType = Cell(row, "Passenger Type")?.Trim();
            var specialNeeds = Cell(row, "Special Needs")?.Trim();
            var hasMobilityRequirement = MtmPassengerMobilityMapper.TryMap(rawPassengerType, specialNeeds, out var mobilityRequirement);
            return new ParsedTripImportItem(
                offset + 2, BrokerTripNumber(Cell(row, "Trip Number")), ExternalMemberId(Cell(row, "Medicaid Number")),
                SourceText(Cell(row, "Member's First Name")), SourceText(Cell(row, "Member's Last Name")),
                hasValidAppointmentDate ? serviceDate : null,
                hasValidAppointmentTime ? LocalTime.FromTimeOnly(appointmentTime) : null,
                SourceText(Cell(row, "Pickup Address")), SourceText(Cell(row, "Pickup City")), SourceText(Cell(row, "Delivery Address")),
                SourceText(Cell(row, "Delivery City")), SourceText(Cell(row, "Trip Status")),
                string.Equals(Cell(row, "Will Call Flag"), "Y", StringComparison.OrdinalIgnoreCase),
                hasMobilityRequirement ? mobilityRequirement : null,
                rawPassengerType,
                hasValidCost ? tripCost : null,
                hasValidMileage ? tripMileage : null,
                hasAppointmentDate && !hasValidAppointmentDate ? $"Appointment Date '{date}' is invalid." : null,
                hasAppointmentTime && !hasValidAppointmentTime ? $"Appointment Time '{time}' is invalid." : null,
                !string.IsNullOrWhiteSpace(cost) && !hasValidCost ? $"Trip Cost '{cost}' is invalid." : null,
                !string.IsNullOrWhiteSpace(mileage) && !hasValidMileage ? $"Trip Mileage '{mileage}' is invalid." : null,
                rawPassengerType is not null && !hasMobilityRequirement
                    ? $"Passenger Type '{rawPassengerType}' is not supported."
                    : null
            );
        }).ToList();
    }

    private static string? BrokerTripNumber(string? sourceValue) => Identifier(sourceValue);

    private static string? ExternalMemberId(string? sourceValue) => Identifier(sourceValue);

    private static string? Identifier(string? sourceValue) =>
        string.IsNullOrWhiteSpace(sourceValue) ? null : sourceValue.Trim().ToUpperInvariant();

    private static string? SourceText(string? sourceValue) =>
        string.IsNullOrWhiteSpace(sourceValue) ? null : sourceValue.Trim();
}
