using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Tenants.Events;

public sealed record TenantMembershipCreatedDomainEvent(Guid MembershipId, string TenantId, Guid UserId) : DomainEvent;
