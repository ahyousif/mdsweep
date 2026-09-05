using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.ResetScheduledPickupTime;

public sealed record ResetScheduledPickupTimeCommand(Guid TripId) : ICommand<Guid>;
