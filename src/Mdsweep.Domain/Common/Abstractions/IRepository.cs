namespace Mdsweep.Domain.Common.Abstractions;

public interface IRepository
{
    Task<TAggregate?> GetByIdAsync<TAggregate, TId>(TId id, CancellationToken ct)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull;

    Task AddAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : AggregateRoot<Guid>;

    Task UpdateAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : AggregateRoot<Guid>;

    Task DeleteAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : AggregateRoot<Guid>;
}
