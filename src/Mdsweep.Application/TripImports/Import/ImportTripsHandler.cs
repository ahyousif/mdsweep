using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.Trips;
using Mdsweep.Domain.TripImports;
using System.Security.Cryptography;

namespace Mdsweep.Application.TripImports.Import;

public sealed class ImportTripsHandler(
    IEnumerable<ITripImportFileParser> parsers,
    ITripImportLookup lookup,
    IRepository repository,
    IClock clock
)
{
    public async Task<Result<ImportTripsResult>> Handle(ImportTripsCommand command, CancellationToken ct)
    {
        var parser = parsers.SingleOrDefault(candidate => candidate.CanParse(command.FileName, command.ContentType));
        if (parser is null)
        {
            return Result.Invalid(
                new ValidationError
                {
                    Identifier = "file",
                    ErrorMessage = "Only CSV and XLSX trip files are supported.",
                }
            );
        }

        IReadOnlyList<ParsedTripImportItem> rows;
        try
        {
            rows = await parser.ParseAsync(command.Content, ct);
        }
        catch (TripImportParseException exception)
        {
            return Result.Invalid(new ValidationError { Identifier = "file", ErrorMessage = exception.Message });
        }

        var duplicates = rows.Where(row => row.TripNumber is not null)
            .GroupBy(row => row.TripNumber!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        var passengers = (
            await lookup.FindPassengersAsync(
                [.. rows.Where(row => row.BrokerMemberId is not null).Select(row => row.BrokerMemberId!).Distinct()],
                ct
            )
        ).ToDictionary(passenger => passenger.BrokerMemberId!, StringComparer.Ordinal);
        var trips = (
            await lookup.FindTripsAsync(
                [.. rows.Where(row => row.TripNumber is not null).Select(row => row.TripNumber!).Distinct()],
                ct
            )
        ).ToDictionary(trip => trip.BrokerTripNumber, StringComparer.Ordinal);

        var problems = new List<TripImportProblem>();
        var validRows = new List<ParsedTripImportItem>();

        foreach (var row in rows)
        {
            var message = Validate(row, duplicates, passengers, trips);
            if (message is null)
            {
                validRows.Add(row);
            }
            else
            {
                problems.Add(new TripImportProblem(row.RowNumber, row.TripNumber, message));
            }
        }

        var added = 0;
        var updated = 0;
        var unchanged = 0;
        var schedulingTripIds = new List<Guid>();

        foreach (var row in validRows)
        {
            if (!passengers.TryGetValue(row.BrokerMemberId!, out var passenger))
            {
                passenger = PassengerAggregate.Create(row.BrokerMemberId, row.FirstName!, row.LastName!);
                await repository.AddAsync(passenger, ct);
                passengers.Add(row.BrokerMemberId!, passenger);
            }
            else
            {
                passenger.ReconcileBrokerIdentity(row.FirstName!, row.LastName!);
            }

            var brokerData = new BrokerTripData(
                row.ServiceDate!.Value,
                row.AppointmentTime,
                row.PickupAddress!,
                row.PickupCity!,
                row.DropoffAddress!,
                row.DropoffCity!,
                row.BrokerStatus,
                row.IsWillCall,
                row.MobilityRequirement!.Value,
                row.RawImportedPassengerType,
                row.TripCost,
                row.TripMileage
            );

            if (!trips.TryGetValue(row.TripNumber!, out var trip))
            {
                trip = TripAggregate.Create(passenger.Id, row.TripNumber!, brokerData);
                await repository.AddAsync(trip, ct);
                trips.Add(row.TripNumber!, trip);
                added++;
                schedulingTripIds.Add(trip.Id);
            }
            else if (trip.BrokerData == brokerData)
            {
                unchanged++;
            }
            else
            {
                var schedulingInputsChanged = SchedulingInputsChanged(trip.BrokerData, brokerData);
                trip.ReconcileBrokerData(brokerData);
                updated++;
                if (schedulingInputsChanged)
                {
                    schedulingTripIds.Add(trip.Id);
                }
            }
        }
        var outcome = new ImportTripsResult(command.FileName, rows.Count, added, updated, unchanged, problems.Count, problems);
        await repository.AddAsync(
            TripImportReceipt.Create(
                command.FileName,
                Convert.ToHexString(SHA256.HashData(command.Content)),
                new ImportOutcome(outcome.Total, outcome.Added, outcome.Updated, outcome.Unchanged, outcome.ProblemCount),
                clock.GetCurrentInstant()
            ),
            ct
        );

        return Result.Success(outcome with { SchedulingTripIds = schedulingTripIds });
    }

    private static string? Validate(
        ParsedTripImportItem row,
        IReadOnlySet<string> duplicates,
        IReadOnlyDictionary<string, PassengerAggregate> passengers,
        IReadOnlyDictionary<string, TripAggregate> trips
    )
    {
        if (row.TripNumber is null)
            return "Trip Number is required.";
        if (duplicates.Contains(row.TripNumber))
            return $"Trip {row.TripNumber} appears more than once in this file.";
        if (row.BrokerMemberId is null)
            return "Medicaid Number is required.";
        if (row.AppointmentDateValidationError is not null)
            return row.AppointmentDateValidationError;
        if (row.ServiceDate is null)
            return "Appointment Date is required.";
        if (row.AppointmentTimeValidationError is not null)
            return row.AppointmentTimeValidationError;
        if (row.TripCostValidationError is not null)
            return row.TripCostValidationError;
        if (row.TripMileageValidationError is not null)
            return row.TripMileageValidationError;
        if (row.PassengerTypeValidationError is not null)
            return row.PassengerTypeValidationError;
        if (row.MobilityRequirement is null)
            return "Passenger Type is required.";
        if (row.FirstName is null)
            return "Member's First Name is required.";
        if (row.LastName is null)
            return "Member's Last Name is required.";
        if (row.PickupAddress is null)
            return "Pickup Address is required for routing.";
        if (row.PickupCity is null)
            return "Pickup City is required for routing.";
        if (row.DropoffAddress is null)
            return "Delivery Address is required for routing.";
        if (row.DropoffCity is null)
            return "Delivery City is required for routing.";
        if (
            trips.TryGetValue(row.TripNumber, out var trip)
            && (!passengers.TryGetValue(row.BrokerMemberId, out var passenger) || trip.PassengerId != passenger.Id)
        )
            return $"Trip {row.TripNumber} already belongs to another passenger.";
        return null;
    }

    private static bool SchedulingInputsChanged(BrokerTripData previous, BrokerTripData current) =>
        previous.ServiceDate != current.ServiceDate
        || previous.AppointmentTime != current.AppointmentTime
        || previous.PickupAddress != current.PickupAddress
        || previous.PickupCity != current.PickupCity
        || previous.DropoffAddress != current.DropoffAddress
        || previous.DropoffCity != current.DropoffCity;
}
