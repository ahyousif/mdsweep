using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Tenants.Events;

namespace Mdsweep.Domain.Tenants;

public sealed class TenantMembership : AggregateRoot<Guid>
{
    private TenantMembership()
        : base(default) { }

    private TenantMembership(Guid id, string tenantId, Guid userId, string role)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }

    public string TenantId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;

    public static TenantMembership Create(string tenantId, Guid userId, string role)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.Default(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(role, nameof(role));

        Guard.Against.Invalid(
            !TenantIdentifier.IsValid(tenantId),
            "Tenant ID must use the xxxx-xxxx-xxxx lowercase unambiguous format."
        );

        var membership = new TenantMembership(Guid.CreateVersion7(), tenantId, userId, role);
        membership.AddDomainEvent(
            new TenantMembershipCreatedDomainEvent(membership.Id, membership.TenantId, membership.UserId)
        );

        return membership;
    }
}
