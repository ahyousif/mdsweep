using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Users.Events;

public sealed record InvitationAcceptedDomainEvent(
    Guid InvitationId,
    string TenantId,
    string KeycloakUserId,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles
) : DomainEvent;
