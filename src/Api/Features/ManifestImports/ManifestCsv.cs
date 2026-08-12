using System.Globalization;

namespace Mdsweep.Api.Features.ManifestImports;

internal static class ManifestCsv
{
    private static readonly string[] RequiredHeaders =
    [
        "Appointment Date", "Delivery Address", "Pickup Address", "Time", "Trip Number", "Trip Status"
    ];

    public static async Task<IReadOnlyList<ManifestPreviewRow>> Preview(Stream source, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(source, leaveOpen: true);
        var records = Parse(await reader.ReadToEndAsync(cancellationToken));
        if (records.Count == 0)
        {
            throw new ManifestFormatException("The file is empty.");
        }

        var headers = records[0];
        var positions = headers
            .Select((header, index) => (Header: header.Trim(), Index: index))
            .GroupBy(x => x.Header, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);
        var missing = RequiredHeaders.Where(header => !positions.ContainsKey(header)).ToArray();
        if (missing.Length > 0)
        {
            throw new ManifestFormatException($"Missing required columns: {string.Join(", ", missing)}.");
        }

        var rows = records.Skip(1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
            .Select(row => Classify(row, positions)).ToArray();
        var duplicateTripNumbers = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.TripNumber))
            .GroupBy(row => row.TripNumber, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return rows.Select(row => duplicateTripNumbers.Contains(row.TripNumber)
            ? row with
            {
                Disposition = ManifestRowDisposition.Blocked,
                Messages = row.Messages.Append("Trip Number appears more than once in this Manifest.").ToArray()
            }
            : row).ToArray();
    }

    private static ManifestPreviewRow Classify(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> positions)
    {
        string Get(string name) => positions[name] < row.Count ? row[positions[name]].Trim() : string.Empty;
        var tripNumber = Get("Trip Number");
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(tripNumber)) messages.Add("Trip Number is missing.");
        var hasDate = DateOnly.TryParseExact(Get("Appointment Date"), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var appointmentDate);
        if (!hasDate)
            messages.Add("Appointment Date must use MM/DD/YYYY.");
        if (string.IsNullOrWhiteSpace(Get("Pickup Address"))) messages.Add("Pickup Address is missing.");
        if (string.IsNullOrWhiteSpace(Get("Delivery Address"))) messages.Add("Delivery Address is missing.");
        var hasTime = TimeOnly.TryParseExact(Get("Time"), "HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var appointmentTime);
        if (!hasTime)
            messages.Add("Time must use four digits such as 0930.");

        var values = new
        {
            AppointmentDate = hasDate ? appointmentDate : (DateOnly?)null,
            AppointmentTime = hasTime ? appointmentTime : (TimeOnly?)null,
            MemberFirstName = Get("Member's First Name"),
            MemberLastName = Get("Member's Last Name"),
            PickupAddress = Get("Pickup Address"),
            PickupCity = Get("Pickup City"),
            DeliveryAddress = Get("Delivery Address"),
            DeliveryCity = Get("Delivery City"),
            PassengerType = Get("Passenger Type"),
            VehicleType = Get("Vehicle Type"),
            BrokerStatus = Get("Trip Status"),
            IsWillCall = Get("Will Call Flag").Equals("Y", StringComparison.OrdinalIgnoreCase)
        };
        if (messages.Count > 0)
        {
            return Row(ManifestRowDisposition.Blocked, messages);
        }

        var status = values.BrokerStatus;
        if (!status.Equals("VALID", StringComparison.OrdinalIgnoreCase))
        {
            return Row(ManifestRowDisposition.Warning, [$"MTM status is {status}; the Trip will remain inactive."]);
        }

        return Row(ManifestRowDisposition.Ready, []);

        ManifestPreviewRow Row(ManifestRowDisposition disposition, IReadOnlyList<string> rowMessages) => new(
            tripNumber, disposition, rowMessages, values.AppointmentDate, values.AppointmentTime,
            values.MemberFirstName, values.MemberLastName, values.PickupAddress, values.PickupCity,
            values.DeliveryAddress, values.DeliveryCity, values.PassengerType, values.VehicleType,
            values.BrokerStatus, values.IsWillCall);
    }

    private static List<List<string>> Parse(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '"')
            {
                if (quoted && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                record.Add(field.ToString());
                field.Clear();
            }
            else if ((character == '\n' || character == '\r') && !quoted)
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                record.Add(field.ToString());
                field.Clear();
                records.Add(record);
                record = [];
            }
            else field.Append(character);
        }
        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }
        return records;
    }
}

internal sealed class ManifestFormatException(string message) : Exception(message);
