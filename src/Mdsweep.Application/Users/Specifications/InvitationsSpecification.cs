using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Specifications;

public sealed class InvitationsSpecification : SpecificationBuilder<InvitationAggregate, Guid, InvitationsSpecification>
{
    public InvitationsSpecification()
    {
        Spec.AddSorting(x => x.ExpiresAt, descending: true);
    }

    public InvitationsSpecification WithEmail(string email)
    {
        Spec.Add(query => query.Where(x => x.Email.ToLower() == email));
        return this;
    }

    public InvitationsSpecification WithStatus(InvitationStatus status)
    {
        Spec.Add(query => query.Where(x => x.Status == status));
        return this;
    }

    public InvitationsSpecification WithTenantId(string tenantId)
    {
        Spec.Add(query => query.Where(x => x.TenantId == tenantId));
        return this;
    }
}
