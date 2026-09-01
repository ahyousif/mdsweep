using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.SetScheduledPickupTime;

public sealed class SetScheduledPickupTimeHandler(IRepository repository)
{
    public async Task<Result<Guid>> Handle(SetScheduledPickupTimeCommand command, CancellationToken ct)
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(command.TripId, ct);

        if (trip is null)
        {
            return Result.NotFound();
        }

        trip.SetScheduledPickupTime(command.ScheduledPickupTime);

        await repository.UpdateAsync(trip, ct);

        return Result.Success(trip.Id);
    }
}
