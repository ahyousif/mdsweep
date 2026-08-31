using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;
using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Application.TripImports.Apply;

public sealed class ApplyTripImportHandler(ITripImportLookup lookup, IRepository repository, IClock clock)
{
    public async Task<Result<TripImportModel>> Handle(ApplyTripImportCommand command, CancellationToken ct)
    {
        var tripImport = await lookup.FindImportAsync(command.Id, ct);
        if (tripImport is null) return Result.NotFound();
        if (tripImport.Status is TripImportStatus.Applied) return Result.Conflict("This trip import has already been applied.");
        if (await lookup.HasAppliedImportAsync(tripImport.ContentFingerprint, ct))
            return Result.Conflict("An identical trip import has already been applied.");

        var rows = tripImport.Rows.Where(row => row.Disposition is not TripImportRowDisposition.Blocked).ToList();
        var passengers = (await lookup.FindPassengersAsync(
            rows.Select(row => row.BrokerMemberId!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), ct
        )).ToDictionary(passenger => passenger.BrokerMemberId!, StringComparer.OrdinalIgnoreCase);
        var trips = (await lookup.FindTripsAsync(
            rows.Select(row => row.TripNumber!).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), ct
        )).ToDictionary(trip => trip.BrokerTripNumber, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (!passengers.TryGetValue(row.BrokerMemberId!, out var passenger))
            {
                passenger = PassengerAggregate.Create(row.BrokerMemberId, row.FirstName!, row.LastName!);
                await repository.AddAsync(passenger, ct);
                passengers.Add(row.BrokerMemberId!, passenger);
            }

            var brokerFacts = new BrokerTripFacts(
                row.ServiceDate!.Value, row.AppointmentTime,
                row.PickupAddress ?? string.Empty, row.PickupCity ?? string.Empty,
                row.DropoffAddress ?? string.Empty, row.DropoffCity ?? string.Empty,
                row.BrokerStatus, row.IsWillCall
            );
            if (!trips.TryGetValue(row.TripNumber!, out var trip))
            {
                trip = TripAggregate.Create(passenger.Id, row.TripNumber!, brokerFacts);
                await repository.AddAsync(trip, ct);
                trips.Add(row.TripNumber!, trip);
            }
            else
            {
                if (trip.PassengerId != passenger.Id)
                    return Result.Conflict("A broker trip number cannot be moved to a different passenger.");
                trip.ReconcileBrokerFacts(brokerFacts);
            }
            row.MarkApplied(trip.Id);
        }

        tripImport.MarkApplied(clock.GetCurrentInstant());
        return Result.Success(TripImportModel.FromAggregate(tripImport));
    }
}
