using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Users.Events;

public sealed record InvitationCancelledDomainEvent(Guid InvitationId) : DomainEvent;
