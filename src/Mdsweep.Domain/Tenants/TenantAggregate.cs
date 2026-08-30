using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Tenants;

public sealed class TenantAggregate : Entity<Guid>
{
    public TenantAggregate()
        : base(default) { }

    public string Name { get; private set; } = null!;
    public string KeycloakOrganizationId { get; private set; } = null!;
}
