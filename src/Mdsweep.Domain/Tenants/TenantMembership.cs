using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Tenants;

public sealed class TenantMembership : Entity<Guid>
{
    public TenantMembership()
        : base(default) { }

    public string TenantId { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = null!;
}
