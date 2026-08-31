using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Tenants.Events;

public sealed record TenantCreatedDomainEvent(string TenantId) : DomainEvent;
