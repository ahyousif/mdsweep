using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Users.Events;

public sealed record InvitationCreatedDomainEvent(
    Guid InvitationId,
    string Email,
    string FirstName,
    string Token,
    Instant ExpiresAt
) : DomainEvent;
