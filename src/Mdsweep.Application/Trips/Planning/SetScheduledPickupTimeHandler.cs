using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Trips;
using JasperFx.MultiTenancy;
using Wolverine.Attributes;

namespace Mdsweep.Application.Trips.Planning;

public static class SetScheduledPickupTimeHandler
{
    [Transactional(typeof(IRepository))]
    public static async Task<Result<SetScheduledPickupTimeResult>> Handle(
        SetScheduledPickupTime command,
        IRepository repository,
        TenantId activeTenant,
        CancellationToken ct
    )
    {
        _ = activeTenant;
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(command.TripId, ct);
        if (trip is null)
            return Result.NotFound();

        trip.SetScheduledPickupTime(command.ScheduledPickupTime);
        await repository.UpdateAsync(trip, ct);
        return Result.Success(new SetScheduledPickupTimeResult(trip.Id, trip.ScheduledPickupTime!.Value));
    }
}
