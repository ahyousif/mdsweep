using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Trips.Events;

public sealed record TripBrokerFactsReconciledDomainEvent(Guid TripId, string BrokerTripNumber)
    : DomainEvent;
