using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Passengers.Events;

public sealed record PassengerCreatedDomainEvent(Guid PassengerId) : DomainEvent;
