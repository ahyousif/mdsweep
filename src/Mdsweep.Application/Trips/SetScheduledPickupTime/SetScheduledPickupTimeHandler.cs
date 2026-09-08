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

        if (command.PickupTime.HasValue)
        {
            trip.OverridePickupTime(command.PickupTime.Value);
        }
        else
        {
            trip.RemovePickupOverride();
        }

        await repository.UpdateAsync(trip, ct);

        return Result.Success(trip.Id);
    }
}
