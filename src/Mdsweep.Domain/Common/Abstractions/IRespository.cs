namespace Mdsweep.Domain.Common.Abstractions;

public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct);

    Task AddAsync(TAggregate aggregate, CancellationToken ct);

    Task UpdateAsync(TAggregate aggregate, CancellationToken ct);

    Task DeleteAsync(TAggregate aggregate, CancellationToken ct);
}
