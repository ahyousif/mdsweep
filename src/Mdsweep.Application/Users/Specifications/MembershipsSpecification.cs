using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Tenants;

namespace Mdsweep.Application.Users.Specifications;

public sealed class MembershipsSpecification : SpecificationBuilder<TenantMembership, Guid, MembershipsSpecification>
{
    public MembershipsSpecification WithTenantId(string tenantId)
    {
        Spec.Add(query => query.Where(x => x.TenantId == tenantId));
        return this;
    }

    public MembershipsSpecification WithUserId(Guid userId)
    {
        Spec.Add(query => query.Where(x => x.UserId == userId));
        return this;
    }
}
