using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Users.Events;

namespace Mdsweep.Domain.Users;

public sealed class UserAggregate : AggregateRoot<Guid>
{
    private UserAggregate()
        : base(default) { }

    private UserAggregate(Guid id, string firstName, string lastName, string keycloakUserId)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        KeycloakUserId = keycloakUserId;
    }

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string KeycloakUserId { get; private set; } = null!;
    public string Email { get; private set; } = null!;

    public static UserAggregate Create(string firstName, string lastName, string keycloakUserId, string email)
    {
        var user = new UserAggregate(
            Guid.CreateVersion7(),
            firstName: Guard.Against.Null(firstName, nameof(firstName)),
            lastName: Guard.Against.Null(lastName, nameof(lastName)),
            keycloakUserId: Guard.Against.Null(keycloakUserId, nameof(keycloakUserId))
        )
        {
            Email = Guard.Against.NullOrWhiteSpace(email, nameof(email)),
        };

        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public void UpdateNames(string firstName, string lastName)
    {
        FirstName = Guard.Against.NullOrWhiteSpace(firstName);
        LastName = Guard.Against.NullOrWhiteSpace(lastName);
    }
}
