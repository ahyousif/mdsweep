using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Trips.Events;

public sealed record TripBrokerDataReconciledDomainEvent(Guid TripId, string BrokerTripNumber) : DomainEvent;
