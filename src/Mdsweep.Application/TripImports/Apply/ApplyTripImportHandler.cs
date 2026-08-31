using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.TripImports.Apply;

public sealed class ApplyTripImportHandler(ITripImportWorkflowStore store, IClock clock)
{
    public async Task<Result<TripImportModel>> Handle(ApplyTripImportCommand command, CancellationToken ct)
    {
        var tripImport = await store.FindImportAsync(command.Id, ct);
        if (tripImport is null) return Result.NotFound();
        if (tripImport.Status is TripImportStatus.Applied) return Result.Conflict("This trip import has already been applied.");

        foreach (var row in tripImport.Rows.Where(row => row.Disposition is not TripImportRowDisposition.Blocked))
        {
            var passenger = await store.FindPassengerByBrokerMemberIdAsync(row.BrokerMemberId!, ct);
            if (passenger is null)
            {
                passenger = PassengerAggregate.Create(row.BrokerMemberId, row.FirstName!, row.LastName!);
                await store.AddPassengerAsync(passenger, ct);
            }

            var brokerFacts = new BrokerTripFacts(
                row.ServiceDate!.Value, row.AppointmentTime,
                row.PickupAddress ?? string.Empty, row.PickupCity ?? string.Empty,
                row.DropoffAddress ?? string.Empty, row.DropoffCity ?? string.Empty,
                row.BrokerStatus, row.IsWillCall
            );
            var trip = await store.FindTripByBrokerTripNumberAsync(row.TripNumber!, ct);
            if (trip is null)
            {
                trip = TripAggregate.Create(passenger.Id, row.TripNumber!, brokerFacts);
                await store.AddTripAsync(trip, ct);
            }
            else
            {
                trip.ReconcileBrokerFacts(brokerFacts);
            }
            row.MarkApplied(trip.Id);
        }

        tripImport.MarkApplied(clock.GetCurrentInstant());
        return Result.Success(TripImportModel.FromAggregate(tripImport));
    }
}
