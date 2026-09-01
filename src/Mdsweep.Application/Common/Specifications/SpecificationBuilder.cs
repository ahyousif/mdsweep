using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Application.Common.Specifications;

public abstract class SpecificationBuilder<TEntity, TId, TSelf>
    where TEntity : Entity<TId>
    where TId : notnull
    where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
{
    internal SpecificationHelper<TEntity> Spec { get; } = new();

    public Specification<TEntity> Build()
    {
        return Spec.Build();
    }

    public Specification<TEntity, TResult> Build<TResult>(Specification<TEntity, TResult> projection)
    {
        return Spec.Build(projection);
    }
}
