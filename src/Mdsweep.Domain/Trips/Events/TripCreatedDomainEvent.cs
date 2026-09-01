using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Trips.Events;

public sealed record TripCreatedDomainEvent(Guid TripId, Guid PassengerId, string BrokerTripNumber)
    : DomainEvent;
