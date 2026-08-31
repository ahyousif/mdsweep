using Mdsweep.Application.Trips;

namespace Mdsweep.Api.Features.Trips;

public sealed record TripResponse(Guid Id, string BrokerTripNumber, LocalTime? ScheduledPickupTime)
{
    public static TripResponse FromModel(TripModel model) =>
        new(model.Id, model.BrokerTripNumber, model.ScheduledPickupTime);
}
