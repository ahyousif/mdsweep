using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Planning;

public sealed class SetScheduledPickupTimeHandler(IRepository repository)
{
    public async Task<Result<SetScheduledPickupTimeResult>> Handle(
        SetScheduledPickupTime command,
        CancellationToken ct
    )
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(command.TripId, ct);
        if (trip is null)
            return Result.NotFound();

        trip.SetScheduledPickupTime(command.ScheduledPickupTime);
        await repository.UpdateAsync(trip, ct);
        return Result.Success(new SetScheduledPickupTimeResult(trip.Id, trip.ScheduledPickupTime!.Value));
    }
}
