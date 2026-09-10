using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Specifications;

public sealed class InvitationsSpecification : Specification<InvitationAggregate, InvitationAggregate>
{
    public InvitationsSpecification(string? tenantId = null, string? email = null)
    {
        if (tenantId is not null)
            Query.Where(x => x.TenantId == tenantId);
        if (email is not null)
            Query.Where(x => x.Email.ToLower() == email && x.Status == "Pending");
        Query.OrderByDescending(x => x.ExpiresAt);
        Query.Select(x => x);
    }
}
