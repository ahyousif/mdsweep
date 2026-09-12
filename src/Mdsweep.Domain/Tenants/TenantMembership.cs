using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;

namespace Mdsweep.Domain.Tenants;

public sealed class TenantMembership : AggregateRoot<Guid>
{
    private TenantMembership()
        : base(default) { }

    private TenantMembership(Guid id, string tenantId, Guid userId, string displayName, bool isActive, string[] roles)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        DisplayName = displayName;
        IsActive = isActive;
        Roles = [.. roles];
    }

    public string TenantId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string[] Roles { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; }

    public void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
    }

    public void SetDisplayName(string value)
    {
        DisplayName = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public void SetRoles(string[] roles)
    {
        if (Roles.Order().SequenceEqual(roles.Order()))
        {
            return;
        }

        Roles = [.. roles];
    }

    public static TenantMembership Create(string tenantId, Guid userId, string displayName, string[] roles)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.Default(userId, nameof(userId));
        Guard.Against.Invalid(!TenantIdentifier.IsValid(tenantId), "Invalid tenant ID.");

        var membership = new TenantMembership(
            Guid.CreateVersion7(),
            tenantId,
            userId,
            displayName,
            isActive: true,
            roles
        );

        return membership;
    }
}
