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

    private TenantAggregate(string id, string name, string keycloakOrganizationId, string? defaultSenderEmail = null)
        : base(id)
    {
        Name = name;
        KeycloakOrganizationId = keycloakOrganizationId;
        DefaultSenderEmail = defaultSenderEmail;
    }

    public string Name { get; private set; } = null!;
    public string KeycloakOrganizationId { get; private set; } = null!;
    public string? DefaultSenderEmail { get; private set; }
    public int PickupBufferMinutes { get; private set; } = DefaultPickupBufferMinutes;

    public static TenantAggregate Create(string tenantId, string name, string keycloakOrganizationId)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.Invalid(
            !TenantIdentifier.IsValid(tenantId),
            "Tenant ID must use the xxxx-xxxx-xxxx lowercase unambiguous format."
        );

        var tenant = new TenantAggregate(
            tenantId,
            Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Guard.Against.NullOrWhiteSpace(keycloakOrganizationId, nameof(keycloakOrganizationId))
        );

        tenant.AddDomainEvent(new TenantCreatedDomainEvent(tenant.Id));

        return tenant;
    }

    public void SetPickupBufferMinutes(int pickupBufferMinutes)
    {
        Guard.Against.NullOrOutOfRange(pickupBufferMinutes, nameof(pickupBufferMinutes), 0, MaximumPickupBufferMinutes);

        PickupBufferMinutes = pickupBufferMinutes;
    }
}
