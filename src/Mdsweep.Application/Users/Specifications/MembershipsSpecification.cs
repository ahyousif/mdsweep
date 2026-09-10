using Mdsweep.Domain.Tenants;

namespace Mdsweep.Application.Users.Specifications;

public sealed class MembershipsSpecification : Specification<TenantMembership, TenantMembership>
{
    public MembershipsSpecification(string? tenantId = null, Guid? userId = null)
    {
        if (tenantId is not null)
            Query.Where(x => x.TenantId == tenantId);
        if (userId.HasValue)
            Query.Where(x => x.UserId == userId.Value);
        Query.Select(x => x);
    }
}
