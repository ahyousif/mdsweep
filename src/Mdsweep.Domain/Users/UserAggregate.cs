using Mdsweep.Domain.Common.Abstractions;

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

    public static UserAggregate Create(string firstName, string lastName, string keycloakUserId)
    {
        Guard.Against.Null(firstName, nameof(firstName));
        Guard.Against.Null(lastName, nameof(lastName));
        Guard.Against.Null(keycloakUserId, nameof(keycloakUserId));

        var user = new UserAggregate(Guid.CreateVersion7(), firstName, lastName, keycloakUserId);

        return user;
    }
}
