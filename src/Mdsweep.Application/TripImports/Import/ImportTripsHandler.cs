using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.TripImports.Import;

public sealed class ImportTripsHandler(
    IEnumerable<ITripImportFileParser> parsers,
    ITripImportLookup lookup,
    IRepository repository
)
{
    public async Task<Result<ImportTripsResult>> Handle(ImportTripsCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.FileName) || command.Content.Length == 0)
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = "A non-empty CSV or XLSX file is required." });

        var parser = parsers.SingleOrDefault(candidate => candidate.CanParse(command.FileName, command.ContentType));
        if (parser is null)
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = "Only CSV and XLSX trip files are supported." });

        IReadOnlyList<ParsedTripImportItem> rows;
        try { rows = await parser.ParseAsync(command.Content, ct); }
        catch (TripImportParseException exception)
        { return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = exception.Message }); }

        var normalizedRows = rows.Select(Normalize).ToList();
        var duplicates = normalizedRows.Where(row => row.TripNumber is not null)
            .GroupBy(row => row.TripNumber!, StringComparer.Ordinal).Where(group => group.Count() > 1)
            .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var passengers = (await lookup.FindPassengersAsync(normalizedRows.Where(row => row.BrokerMemberId is not null)
            .Select(row => row.BrokerMemberId!).Distinct().ToList(), ct)).ToDictionary(passenger => passenger.BrokerMemberId!, StringComparer.Ordinal);
        var trips = (await lookup.FindTripsAsync(normalizedRows.Where(row => row.TripNumber is not null)
            .Select(row => row.TripNumber!).Distinct().ToList(), ct)).ToDictionary(trip => trip.BrokerTripNumber, StringComparer.Ordinal);

        var problems = new List<TripImportProblem>();
        var validRows = new List<ParsedTripImportItem>();
        foreach (var row in normalizedRows)
        {
            var message = Validate(row, duplicates, passengers, trips);
            if (message is null) validRows.Add(row);
            else problems.Add(new TripImportProblem(row.RowNumber, row.TripNumber, message));
        }

        var added = 0; var updated = 0; var unchanged = 0;
        foreach (var row in validRows)
        {
            if (!passengers.TryGetValue(row.BrokerMemberId!, out var passenger))
            {
                passenger = PassengerAggregate.Create(row.BrokerMemberId, row.FirstName!, row.LastName!);
                await repository.AddAsync(passenger, ct);
                passengers.Add(row.BrokerMemberId!, passenger);
            }
            var brokerData = new BrokerTripData(row.ServiceDate!.Value, row.AppointmentTime, row.PickupAddress!, row.PickupCity!, row.DropoffAddress!, row.DropoffCity!, row.BrokerStatus, row.IsWillCall);
            if (!trips.TryGetValue(row.TripNumber!, out var trip))
            {
                trip = TripAggregate.Create(passenger.Id, row.TripNumber!, brokerData);
                await repository.AddAsync(trip, ct);
                trips.Add(row.TripNumber!, trip); added++;
            }
            else if (trip.BrokerData == brokerData) unchanged++;
            else { trip.ReconcileBrokerData(brokerData); updated++; }
        }
        return Result.Success(new ImportTripsResult(command.FileName, normalizedRows.Count, added, updated, unchanged, problems.Count, problems));
    }

    private static ParsedTripImportItem Normalize(ParsedTripImportItem row) => row with
    {
        TripNumber = NormalizeId(row.TripNumber), BrokerMemberId = NormalizeId(row.BrokerMemberId),
        FirstName = Trim(row.FirstName), LastName = Trim(row.LastName), PickupAddress = Trim(row.PickupAddress),
        PickupCity = Trim(row.PickupCity), DropoffAddress = Trim(row.DropoffAddress), DropoffCity = Trim(row.DropoffCity), BrokerStatus = Trim(row.BrokerStatus),
    };
    private static string? NormalizeId(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Validate(ParsedTripImportItem row, IReadOnlySet<string> duplicates,
        IReadOnlyDictionary<string, PassengerAggregate> passengers, IReadOnlyDictionary<string, TripAggregate> trips)
    {
        if (row.TripNumber is null) return "Trip Number is required.";
        if (duplicates.Contains(row.TripNumber)) return $"Trip {row.TripNumber} appears more than once in this file.";
        if (row.BrokerMemberId is null) return "Medicaid Number is required.";
        if (row.AppointmentDateValidationError is not null) return row.AppointmentDateValidationError;
        if (row.ServiceDate is null) return "Appointment Date is required.";
        if (row.AppointmentTimeValidationError is not null) return row.AppointmentTimeValidationError;
        if (row.FirstName is null) return "Member's First Name is required.";
        if (row.LastName is null) return "Member's Last Name is required.";
        if (row.PickupAddress is null) return "Pickup Address is required for routing.";
        if (row.PickupCity is null) return "Pickup City is required for routing.";
        if (row.DropoffAddress is null) return "Delivery Address is required for routing.";
        if (row.DropoffCity is null) return "Delivery City is required for routing.";
        if (trips.TryGetValue(row.TripNumber, out var trip) && (!passengers.TryGetValue(row.BrokerMemberId, out var passenger) || trip.PassengerId != passenger.Id))
            return $"Trip {row.TripNumber} already belongs to another passenger.";
        return null;
    }
}
