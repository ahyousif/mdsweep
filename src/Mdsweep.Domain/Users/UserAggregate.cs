using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Users.Events;

namespace Mdsweep.Domain.Users;

public sealed class UserAggregate : AggregateRoot<Guid>
{
    private readonly List<AccessHistoryEntry> history = [];
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
    public string TenantId { get; private set; } = null!;
    public string? Email { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int Version { get; private set; }
    public IReadOnlyCollection<AccessHistoryEntry> History => history.AsReadOnly();

    public static UserAggregate Create(string firstName, string lastName, string keycloakUserId, string tenantId, string? email = null)
    {
        Guard.Against.Null(firstName, nameof(firstName));
        Guard.Against.Null(lastName, nameof(lastName));
        Guard.Against.Null(keycloakUserId, nameof(keycloakUserId));

        var user = new UserAggregate(Guid.CreateVersion7(), firstName, lastName, keycloakUserId);
        user.TenantId = Guard.Against.NullOrWhiteSpace(tenantId);
        user.Email = email;

        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public void UpdateNames(string firstName, string lastName)
    {
        FirstName = Guard.Against.NullOrWhiteSpace(firstName);
        LastName = Guard.Against.NullOrWhiteSpace(lastName);
    }

    public void SetActive(bool active) => IsActive = active;

    public void Record(string actorSubject, string action, Instant at, string? details = null)
    {
        history.Add(new AccessHistoryEntry(actorSubject, action, at, details));
        Version++;
    }
}
