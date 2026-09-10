using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Specifications;

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
