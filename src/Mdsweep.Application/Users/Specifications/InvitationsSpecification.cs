using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Specifications;

public sealed class InvitationsSpecification : SpecificationBuilder<InvitationAggregate, Guid, InvitationsSpecification>
{
    public InvitationsSpecification()
    {
        Spec.AddSorting(x => x.ExpiresAt, descending: true);
    }

    public InvitationsSpecification WithTenantId(string tenantId)
    {
        Spec.Add(query => query.Where(x => x.TenantId == tenantId));
        return this;
    }

    public InvitationsSpecification WithPendingEmail(string email)
    {
        Spec.Add(query => query.Where(x => x.Email.ToLower() == email && x.Status == "Pending"));
        return this;
    }
}
