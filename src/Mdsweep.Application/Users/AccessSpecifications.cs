using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class UsersSpecification : Specification<UserAggregate, UserAggregate>
{
    public UsersSpecification(string? subject = null, string? email = null, Guid[]? userIds = null)
    {
        if (userIds is not null)
            Query.Where(x => userIds.Contains(x.Id));
        if (subject is not null)
            Query.Where(x => x.KeycloakUserId == subject);
        if (email is not null)
            Query.Where(x => x.Email.ToLower() == email);
        Query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName);
        Query.Select(x => x);
    }
}

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
