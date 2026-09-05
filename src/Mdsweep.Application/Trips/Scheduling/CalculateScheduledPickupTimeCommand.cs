using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.Scheduling;

public sealed record CalculateScheduledPickupTimeCommand(Guid TripId) : ICommand<Guid>;
