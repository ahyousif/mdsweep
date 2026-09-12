using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Specifications;

public sealed class UsersSpecification : SpecificationBuilder<UserAggregate, Guid, UsersSpecification>
{
    public UsersSpecification()
    {
        Spec.AddSorting(x => x.LastName);
        Spec.AddSorting(x => x.FirstName);
    }

    public UsersSpecification WithKeycloakUserId(string id)
    {
        Spec.Add(query => query.Where(x => x.KeycloakUserId == id));
        return this;
    }

    public UsersSpecification WithEmail(string email)
    {
        Spec.Add(query => query.Where(x => x.Email.ToLower() == email));
        return this;
    }
}
