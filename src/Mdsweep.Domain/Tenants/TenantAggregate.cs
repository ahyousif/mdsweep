using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Tenants.Events;

namespace Mdsweep.Domain.Tenants;

public sealed class TenantAggregate : AggregateRoot<string>
{
    private TenantAggregate()
        : base(string.Empty) { }

    private TenantAggregate(string id, string name, string keycloakOrganizationId)
        : base(id)
    {
        Name = name;
        KeycloakOrganizationId = keycloakOrganizationId;
    }

    public string Name { get; private set; } = null!;
    public string KeycloakOrganizationId { get; private set; } = null!;

    public static TenantAggregate Create(string tenantId, string name, string keycloakOrganizationId)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(keycloakOrganizationId, nameof(keycloakOrganizationId));

        Guard.Against.Invalid(
            !TenantIdentifier.IsValid(tenantId),
            "Tenant ID must use the xxxx-xxxx-xxxx lowercase unambiguous format."
        );

        var tenant = new TenantAggregate(tenantId, name, keycloakOrganizationId);
        tenant.AddDomainEvent(new TenantCreatedDomainEvent(tenant.Id));

        return tenant;
    }
}
