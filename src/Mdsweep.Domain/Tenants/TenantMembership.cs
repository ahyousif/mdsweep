using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Tenants.Events;
using Mdsweep.Domain.Users;

namespace Mdsweep.Domain.Tenants;

public sealed class TenantMembership : AggregateRoot<Guid>
{
    private readonly List<AccessHistoryEntry> history = [];

    private TenantMembership()
        : base(default) { }

    private TenantMembership(Guid id, string tenantId, Guid userId, string[] roles)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Roles = roles.ToArray();
    }

    public string TenantId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string[] Roles { get; private set; } = null!;

    public bool IsActive { get; private set; } = true;
    public int Version { get; private set; }
    public IReadOnlyCollection<AccessHistoryEntry> History => history.AsReadOnly();

    public void SetActive(bool active) => IsActive = active;

    public void Record(string actorSubject, string action, Instant at, string? details = null)
    {
        history.Add(new AccessHistoryEntry(actorSubject, action, at, details));
        Version++;
    }

    public void SetRoles(string[] roles)
    {
        Guard.Against.Invalid(!AreValidRoles(roles), "Select one or two distinct roles.");
        Roles = roles.ToArray();
    }

    public static bool AreValidRoles(string[]? roles) =>
        roles is { Length: >= 1 and <= 2 }
        && roles.All(role => role is "Administrator" or "Dispatcher" or "Driver")
        && roles.Distinct().Count() == roles.Length;

    public static TenantMembership Create(string tenantId, Guid userId, string role) =>
        Create(tenantId, userId, [role]);

    public static TenantMembership Create(string tenantId, Guid userId, string[] roles)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.Default(userId, nameof(userId));

        Guard.Against.Invalid(!AreValidRoles(roles), "Select one or two distinct roles.");

        Guard.Against.Invalid(
            !TenantIdentifier.IsValid(tenantId),
            "Tenant ID must use the xxxx-xxxx-xxxx lowercase unambiguous format."
        );

        var membership = new TenantMembership(Guid.CreateVersion7(), tenantId, userId, roles);
        membership.AddDomainEvent(
            new TenantMembershipCreatedDomainEvent(membership.Id, membership.TenantId, membership.UserId)
        );

        return membership;
    }
}
