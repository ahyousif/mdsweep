using Mdsweep.Application.Common.Persistence;
using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.TripImports.Apply;

public sealed class ApplyTripImportHandler(ITripImportLookup lookup, IRepository repository, IClock clock)
{
    public async Task<Result<TripImportModel>> Handle(ApplyTripImportCommand command, CancellationToken ct)
    {
        var tripImport = await lookup.FindImportAsync(command.Id, ct);
        if (tripImport is null)
            return Result.NotFound();
        if (tripImport.Status is TripImportStatus.Applied)
            return Result.Conflict("This trip import has already been applied.");
        if (await lookup.HasAppliedImportAsync(tripImport.ContentFingerprint, ct))
            return Result.Conflict("An identical trip import has already been applied.");

        var items = tripImport.Items.Where(item => item.Disposition is not TripImportItemDisposition.Blocked).ToList();
        var ownershipCandidates = tripImport
            .Items.Where(item => item.TripNumber is not null && item.BrokerMemberId is not null)
            .ToList();
        var passengers = (
            await lookup.FindPassengersAsync(
                ownershipCandidates
                    .Select(item => item.BrokerMemberId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ct
            )
        ).ToDictionary(passenger => passenger.BrokerMemberId!, StringComparer.OrdinalIgnoreCase);
        var trips = (
            await lookup.FindTripsAsync(
                ownershipCandidates
                    .Select(item => item.TripNumber!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                ct
            )
        ).ToDictionary(trip => trip.BrokerTripNumber, StringComparer.OrdinalIgnoreCase);

        // Phase 1: inspect every applicable row before touching an aggregate. Wolverine
        // saves tracked changes after a successful result, including a Result.Conflict.
        foreach (var item in items)
        {
            if (
                item.TripNumber is null
                || item.BrokerMemberId is null
                || item.FirstName is null
                || item.LastName is null
                || item.ServiceDate is null
            )
                return Result.Invalid(
                    new ValidationError
                    {
                        Identifier = "tripImport",
                        ErrorMessage = $"Item {item.RowNumber} is incomplete and cannot be applied.",
                    }
                );
        }

        // Re-check ownership even when Preview displayed the row as blocked. The
        // preview is feedback; this is the persisted-data invariant.
        foreach (var item in ownershipCandidates)
        {
            if (
                trips.TryGetValue(item.TripNumber!, out var trip)
                && (
                    !passengers.TryGetValue(item.BrokerMemberId!, out var passenger) || trip.PassengerId != passenger.Id
                )
            )
            {
                return Result.Conflict($"Trip {item.TripNumber} already belongs to a different passenger.");
            }
        }

        // Phase 2: validation is complete, so aggregate changes are safe to track.
        foreach (var item in items)
        {
            if (!passengers.TryGetValue(item.BrokerMemberId!, out var passenger))
            {
                passenger = PassengerAggregate.Create(item.BrokerMemberId, item.FirstName!, item.LastName!);
                await repository.AddAsync(passenger, ct);
                passengers.Add(item.BrokerMemberId!, passenger);
            }

            var brokerFacts = new BrokerTripData(
                item.ServiceDate!.Value,
                item.AppointmentTime,
                item.PickupAddress ?? string.Empty,
                item.PickupCity ?? string.Empty,
                item.DropoffAddress ?? string.Empty,
                item.DropoffCity ?? string.Empty,
                item.BrokerStatus,
                item.IsWillCall
            );
            if (!trips.TryGetValue(item.TripNumber!, out var trip))
            {
                trip = TripAggregate.Create(passenger.Id, item.TripNumber!, brokerFacts);
                await repository.AddAsync(trip, ct);
                trips.Add(item.TripNumber!, trip);
            }
            else
            {
                trip.ReconcileBrokerData(brokerFacts);
            }
            item.MarkApplied(trip.Id);
        }

        tripImport.MarkApplied(clock.GetCurrentInstant());
        return Result.Success(TripImportModel.FromAggregate(tripImport));
    }
}
