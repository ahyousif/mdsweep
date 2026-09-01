using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Application.Common.Persistence;

/// <summary>
/// The shared aggregate persistence seam. It remains non-generic because the
/// Wolverine EF Core transaction abstraction is implemented by ApplicationDbContext.
/// </summary>
public interface IRepository
{
    Task<TAggregate?> GetByIdAsync<TAggregate, TId>(TId id, CancellationToken ct)
        where TAggregate : AggregateRoot<TId>
        where TId : notnull;

    Task<List<TResult>> ListAsync<TAggregate, TResult>(
        ISpecification<TAggregate, TResult> specification,
        CancellationToken ct
    )
        where TAggregate : class, IAggregateRoot;

    Task<int> CountAsync<TAggregate>(ISpecification<TAggregate> specification, CancellationToken ct)
        where TAggregate : class, IAggregateRoot;

    Task AddAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : class, IAggregateRoot;

    Task UpdateAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : class, IAggregateRoot;

    Task DeleteAsync<TAggregate>(TAggregate aggregate, CancellationToken ct)
        where TAggregate : class, IAggregateRoot;
}
