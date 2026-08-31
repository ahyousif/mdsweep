using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Users.Events;

public sealed record UserCreatedDomainEvent(Guid UserId) : DomainEvent;
