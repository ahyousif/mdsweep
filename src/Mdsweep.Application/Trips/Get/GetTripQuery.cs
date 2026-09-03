using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.Get;

public sealed record GetTripQuery(Guid Id) : IQuery<TripModel>;
