using Mdsweep.Application.Trips.Import.Manifest;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Infrastructure.Manifests;

internal static class MtmManifestParser
{
    private static readonly string[] _requiredHeaders =
    [
        "Trip Number",
        "Medicaid Number",
        "Member's First Name",
        "Member's Last Name",
        "Appointment Date",
        "Trip Type",
        "Pickup Address",
        "Pickup City",
        "Delivery Address",
        "Delivery City",
    ];

    private static readonly string[] _dateFormats = ["M/d/yyyy", "MM/dd/yyyy", "M/d/yy", "yyyy-MM-dd"];

    private static readonly string[] _timeFormats = ["HHmm", "Hmm", "H:mm", "HH:mm", "h:mm tt", "hh:mm tt"];

    public static MtmManifestReadResult Parse(IReadOnlyList<IReadOnlyList<string>> table)
    {
        if (table.Count == 0)
        {
            return new MtmManifestReadResult([], [new MtmManifestProblem(null, null, null, "The manifest is empty.")]);
        }

        var headers = ReadHeaders(table[0]);

        var missingHeaders = _requiredHeaders.Where(header => !headers.ContainsKey(header)).ToList();

        if (missingHeaders.Count > 0)
        {
            return new MtmManifestReadResult(
                [],
                [
                    new MtmManifestProblem(
                        null,
                        null,
                        null,
                        $"The manifest is missing required columns: {string.Join(", ", missingHeaders)}."
                    ),
                ]
            );
        }

        var rows = new List<MtmManifestRow>();
        var problems = new List<MtmManifestProblem>();

        for (var index = 1; index < table.Count; index++)
        {
            var row = table[index];
            var rowNumber = index + 1;
            var problemCountBeforeRow = problems.Count;

            var tripNumber = Identifier(Cell(row, headers, "Trip Number"));

            var memberId = Identifier(Cell(row, headers, "Medicaid Number"));

            var firstName = Cell(row, headers, "Member's First Name");

            var lastName = Cell(row, headers, "Member's Last Name");

            var pickupAddress = Cell(row, headers, "Pickup Address");

            var pickupCity = Cell(row, headers, "Pickup City");

            var dropoffAddress = Cell(row, headers, "Delivery Address");

            var dropoffCity = Cell(row, headers, "Delivery City");

            var pickupState = Cell(row, headers, "Pickup State");

            var pickupZip = Cell(row, headers, "Pickup Zip Code");

            var dropoffState = Cell(row, headers, "Delivery State");

            var dropoffZip = Cell(row, headers, "Delivery Zip Code");

            var brokerStatus = Cell(row, headers, "Trip Status");

            var passengerType = Cell(row, headers, "Passenger Type");

            var specialNeeds = Cell(row, headers, "Special Needs");

            var tripCost = ParseDecimal(Cell(row, headers, "Trip Cost"), rowNumber, tripNumber, "TripCost", problems);

            var tripMileage = ParseDecimal(
                Cell(row, headers, "Trip Mileage"),
                rowNumber,
                tripNumber,
                "TripMileage",
                problems
            );

            Require(problems, rowNumber, tripNumber, "TripNumber", tripNumber, "Trip number is required.");

            Require(problems, rowNumber, tripNumber, "MemberId", memberId, "Medicaid number is required.");

            Require(problems, rowNumber, tripNumber, "FirstName", firstName, "Member first name is required.");

            Require(problems, rowNumber, tripNumber, "LastName", lastName, "Member last name is required.");

            Require(problems, rowNumber, tripNumber, "PickupAddress", pickupAddress, "Pickup address is required.");

            Require(problems, rowNumber, tripNumber, "PickupCity", pickupCity, "Pickup city is required.");

            Require(problems, rowNumber, tripNumber, "DropoffAddress", dropoffAddress, "Delivery address is required.");

            Require(problems, rowNumber, tripNumber, "DropoffCity", dropoffCity, "Delivery city is required.");

            var serviceDate = ParseDate(Cell(row, headers, "Appointment Date"), rowNumber, tripNumber, problems);

            var direction = ParseDirection(Cell(row, headers, "Trip Type"), rowNumber, tripNumber, problems);

            var rawTime = Cell(row, headers, "Time");

            var time = ParseTime(rawTime, rowNumber, tripNumber, problems);

            var isWillCall = ParseWillCall(Cell(row, headers, "Will Call Flag"), rowNumber, tripNumber, problems);

            if (string.IsNullOrWhiteSpace(rawTime))
            {
                if (direction == TripDirection.To)
                {
                    problems.Add(
                        new MtmManifestProblem(
                            rowNumber,
                            tripNumber,
                            "Time",
                            "Appointment time is required for a trip to the appointment."
                        )
                    );
                }

                if (direction == TripDirection.From && isWillCall == false)
                {
                    problems.Add(
                        new MtmManifestProblem(
                            rowNumber,
                            tripNumber,
                            "Time",
                            "Pickup time is required for a scheduled return trip."
                        )
                    );
                }
            }

            if (problems.Count == problemCountBeforeRow)
            {
                rows.Add(
                    new MtmManifestRow(
                        RowNumber: rowNumber,
                        TripNumber: tripNumber!,
                        MemberId: memberId!,
                        FirstName: firstName!,
                        LastName: lastName!,
                        ServiceDate: serviceDate!.Value,
                        Time: time,
                        Direction: direction!.Value,
                        IsWillCall: isWillCall!.Value,
                        PickupAddress: pickupAddress!,
                        PickupCity: pickupCity!,
                        PickupState: pickupState,
                        PickupZip: pickupZip,
                        DropoffAddress: dropoffAddress!,
                        DropoffCity: dropoffCity!,
                        DropoffState: dropoffState,
                        DropoffZip: dropoffZip,
                        BrokerStatus: brokerStatus,
                        PassengerType: passengerType,
                        SpecialNeeds: specialNeeds,
                        TripCost: tripCost,
                        TripMileage: tripMileage
                    )
                );
            }
        }

        return new MtmManifestReadResult([], problems);
    }

    private static Dictionary<string, int> ReadHeaders(IReadOnlyList<string> row)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < row.Count; index++)
        {
            var header = row[index].Trim();

            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            headers.TryAdd(header, index);
        }

        return headers;
    }

    private static string? Cell(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headers, string header)
    {
        if (!headers.TryGetValue(header, out var index) || index >= row.Count)
        {
            return null;
        }

        var value = row[index].Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? Identifier(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static void Require(
        List<MtmManifestProblem> problems,
        int rowNumber,
        string? tripNumber,
        string field,
        string? value,
        string message
    )
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        problems.Add(new MtmManifestProblem(rowNumber, tripNumber, field, message));
    }

    private static LocalDate? ParseDate(
        string? value,
        int rowNumber,
        string? tripNumber,
        List<MtmManifestProblem> problems
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add(new MtmManifestProblem(rowNumber, tripNumber, "ServiceDate", "Appointment date is required."));

            return null;
        }

        foreach (var format in _dateFormats)
        {
            var pattern = LocalDatePattern.CreateWithInvariantCulture(format);

            var result = pattern.Parse(value);

            if (result.Success)
            {
                return result.Value;
            }
        }

        problems.Add(
            new MtmManifestProblem(rowNumber, tripNumber, "ServiceDate", $"Appointment date '{value}' is invalid.")
        );

        return null;
    }

    private static decimal? ParseDecimal(
        string? value,
        int rowNumber,
        string? tripNumber,
        string field,
        List<MtmManifestProblem> problems
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (
            decimal.TryParse(
                value,
                NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out var result
            )
        )
        {
            return result;
        }

        problems.Add(new MtmManifestProblem(rowNumber, tripNumber, field, $"Value '{value}' is invalid."));

        return null;
    }

    private static TripDirection? ParseDirection(
        string? value,
        int rowNumber,
        string? tripNumber,
        List<MtmManifestProblem> problems
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            problems.Add(new MtmManifestProblem(rowNumber, tripNumber, "Direction", "Trip type is required."));

            return null;
        }

        switch (value.Trim().ToUpperInvariant())
        {
            case "T":
                return TripDirection.To;

            case "F":
                return TripDirection.From;

            default:
                problems.Add(
                    new MtmManifestProblem(
                        rowNumber,
                        tripNumber,
                        "Direction",
                        $"Trip type '{value}' is not recognized."
                    )
                );

                return null;
        }
    }

    private static LocalTime? ParseTime(
        string? value,
        int rowNumber,
        string? tripNumber,
        List<MtmManifestProblem> problems
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (
            TimeOnly.TryParseExact(
                value,
                _timeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var time
            )
        )
        {
            return LocalTime.FromTimeOnly(time);
        }

        problems.Add(new MtmManifestProblem(rowNumber, tripNumber, "Time", $"Time '{value}' is invalid."));

        return null;
    }

    private static bool? ParseWillCall(
        string? value,
        int rowNumber,
        string? tripNumber,
        List<MtmManifestProblem> problems
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToUpperInvariant())
        {
            case "Y":
                return true;

            case "N":
                return false;

            default:
                problems.Add(
                    new MtmManifestProblem(
                        rowNumber,
                        tripNumber,
                        "IsWillCall",
                        $"Will Call value '{value}' is not recognized."
                    )
                );

                return null;
        }
    }
}
