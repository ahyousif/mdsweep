using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Tenants.Events;

namespace Mdsweep.Domain.Tenants;

public sealed class TenantAggregate : AggregateRoot<string>
{
    public const int DefaultPickupBufferMinutes = 15;
    public const int MaximumPickupBufferMinutes = 120;

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
    public int PickupBufferMinutes { get; private set; } = DefaultPickupBufferMinutes;

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

    public void SetPickupBufferMinutes(int pickupBufferMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pickupBufferMinutes, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pickupBufferMinutes, MaximumPickupBufferMinutes);

        PickupBufferMinutes = pickupBufferMinutes;
    }
}
