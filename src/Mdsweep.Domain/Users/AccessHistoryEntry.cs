using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Users;

public sealed class AccessHistoryEntry : Entity<Guid>
{
    private AccessHistoryEntry() : base(default) { }

    public AccessHistoryEntry(string actorSubject, string action, Instant occurredAt, string? details = null)
        : base(Guid.CreateVersion7())
    {
        ActorSubject = Guard.Against.NullOrWhiteSpace(actorSubject);
        Action = Guard.Against.NullOrWhiteSpace(action);
        OccurredAt = occurredAt;
        Details = details;
    }

    public string ActorSubject { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public Instant OccurredAt { get; private set; }
    public string? Details { get; private set; }
}
