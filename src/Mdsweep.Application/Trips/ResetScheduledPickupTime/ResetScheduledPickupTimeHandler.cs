using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.ResetScheduledPickupTime;

public sealed class ResetScheduledPickupTimeHandler(IRepository repository)
{
    public async Task<Result<Guid>> Handle(ResetScheduledPickupTimeCommand command, CancellationToken ct)
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(command.TripId, ct);
        if (trip is null)
        {
            return Result.NotFound();
        }

        if (!trip.ResetScheduledPickupToCalculated())
        {
            return Result.Invalid(new ValidationError { ErrorMessage = "No calculated pickup time is available." });
        }

        await repository.UpdateAsync(trip, ct);
        return Result.Success(trip.Id);
    }
}
