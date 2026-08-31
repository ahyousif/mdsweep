using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Passengers.Get;

public sealed record GetPassengerQuery(Guid Id) : IQuery<PassengerModel>;
