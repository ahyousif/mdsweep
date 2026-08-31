namespace Mdsweep.Domain.Common.Abstractions;

public interface IAggregateRoot
{
    IReadOnlyCollection<DomainEvent> DequeueDomainEvents();
}

public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id), IAggregateRoot
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];
    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public IReadOnlyCollection<DomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}
