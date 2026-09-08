using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.SetScheduledPickupTime;

public sealed record SetScheduledPickupTimeCommand(Guid TripId, LocalTime? PickupTime) : ICommand<Guid>;
