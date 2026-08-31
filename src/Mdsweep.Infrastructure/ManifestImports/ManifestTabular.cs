using System.Globalization;
using Mdsweep.Domain.ManifestImports;

namespace Mdsweep.Infrastructure.ManifestImports;

public static class ManifestTabular
{
    internal static readonly string[] RequiredHeaders =
    [
        "Appointment Date",
        "Delivery Address",
        "Pickup Address",
        "Time",
        "Trip Number",
        "Medicaid Number",
        "Trip Status",
    ];

    public static IReadOnlyList<ManifestReceiptRow> Preview(
        IReadOnlyList<IReadOnlyList<string>> records
    )
    {
        if (records.Count == 0)
            throw new ManifestFormatException("The file is empty.");

        var positions = records[0]
            .Select((header, index) => (Header: header, Index: index))
            .GroupBy(x => x.Header, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);
        var missing = RequiredHeaders.Where(header => !positions.ContainsKey(header)).ToArray();
        if (missing.Length > 0)
            throw new ManifestFormatException(
                $"Missing required columns: {string.Join(", ", missing)}."
            );

        var rows = records
            .Skip(1)
            .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
            .Select(row => Classify(row, positions))
            .ToArray();
        var duplicateTripNumbers = rows.Where(row => !string.IsNullOrWhiteSpace(row.TripNumber))
            .GroupBy(row => row.TripNumber, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return rows.Select(row =>
                duplicateTripNumbers.Contains(row.TripNumber)
                    ? row with
                    {
                        Disposition = ManifestRowDisposition.Blocked,
                        Messages = row
                            .Messages.Append("Trip Number appears more than once in this Manifest.")
                            .ToArray(),
                    }
                    : row
            )
            .ToArray();
    }

    private static ManifestReceiptRow Classify(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> positions
    )
    {
        string Get(string name) =>
            positions.TryGetValue(name, out var index) && index < row.Count
                ? row[index]
                : string.Empty;
        var tripNumber = Get("Trip Number");
        var brokerMemberId = Get("Medicaid Number");
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(tripNumber))
            messages.Add("Trip Number is missing.");
        if (string.IsNullOrWhiteSpace(brokerMemberId))
            messages.Add("Medicaid Number is missing.");
        if (string.IsNullOrWhiteSpace(Get("Member's First Name")))
            messages.Add("Member's First Name is missing.");
        if (string.IsNullOrWhiteSpace(Get("Member's Last Name")))
            messages.Add("Member's Last Name is missing.");
        var hasDate = DateOnly.TryParseExact(
            Get("Appointment Date"),
            "MM/dd/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var appointmentDate
        );
        if (!hasDate)
            messages.Add("Appointment Date must use MM/DD/YYYY.");
        if (string.IsNullOrWhiteSpace(Get("Pickup Address")))
            messages.Add("Pickup Address is missing.");
        if (string.IsNullOrWhiteSpace(Get("Delivery Address")))
            messages.Add("Delivery Address is missing.");
        var hasTime = TimeOnly.TryParseExact(
            Get("Time"),
            "HHmm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var appointmentTime
        );
        if (!hasTime)
            messages.Add("Time must use four digits such as 0930.");

        var values = new
        {
            AppointmentDate = hasDate ? appointmentDate : (DateOnly?)null,
            AppointmentTime = hasTime ? appointmentTime : (TimeOnly?)null,
            BrokerMemberId = brokerMemberId,
            MemberFirstName = Get("Member's First Name"),
            MemberLastName = Get("Member's Last Name"),
            PickupAddress = Get("Pickup Address"),
            PickupCity = Get("Pickup City"),
            DeliveryAddress = Get("Delivery Address"),
            DeliveryCity = Get("Delivery City"),
            PassengerType = Get("Passenger Type"),
            VehicleType = Get("Vehicle Type"),
            BrokerStatus = Get("Trip Status"),
            IsWillCall = Get("Will Call Flag").Equals("Y", StringComparison.OrdinalIgnoreCase),
        };
        if (messages.Count > 0)
            return Row(ManifestRowDisposition.Blocked, messages);

        return values.BrokerStatus.Equals("VALID", StringComparison.OrdinalIgnoreCase)
            ? Row(ManifestRowDisposition.Ready, [])
            : Row(
                ManifestRowDisposition.Warning,
                [$"MTM status is {values.BrokerStatus}; the Trip will remain inactive."]
            );

        ManifestReceiptRow Row(
            ManifestRowDisposition disposition,
            IReadOnlyList<string> rowMessages
        ) =>
            new(
                tripNumber,
                values.BrokerMemberId,
                disposition,
                rowMessages,
                values.AppointmentDate,
                values.AppointmentTime,
                values.MemberFirstName,
                values.MemberLastName,
                values.PickupAddress,
                values.PickupCity,
                values.DeliveryAddress,
                values.DeliveryCity,
                values.PassengerType,
                values.VehicleType,
                values.BrokerStatus,
                values.IsWillCall,
                disposition == ManifestRowDisposition.Blocked
                    ? ManifestBrokerChange.Blocked
                    : ManifestBrokerChange.New
            );
    }
}
