namespace Mdsweep.Application.Common.Specifications;

public sealed class SpecificationHelper<TEntity>
    where TEntity : class
{
    private readonly List<Action<ISpecificationBuilder<TEntity>>> _steps = [];
    private readonly List<SpecificationSort<TEntity>> _sorting = [];

    public void Add(Action<ISpecificationBuilder<TEntity>> step)
    {
        _steps.Add(step);
    }

    public void AddSorting(Expression<Func<TEntity, object?>> keySelector, bool descending = false)
    {
        _sorting.Add(new SpecificationSort<TEntity>(keySelector, descending));
    }

    public Specification<TEntity> Build()
    {
        var specification = new Specification<TEntity>();

        foreach (var step in _steps)
        {
            step(specification.Query);
        }

        ApplySorting(specification.Query);

        return specification;
    }

    public Specification<TEntity, TResult> Build<TResult>(Specification<TEntity, TResult> projection)
    {
        return Build().WithProjectionOf(projection);
    }

    private void ApplySorting(ISpecificationBuilder<TEntity> query)
    {
        if (_sorting.Count == 0)
        {
            return;
        }

        var first = _sorting[0];

        IOrderedSpecificationBuilder<TEntity> ordered = first.Descending
            ? query.OrderByDescending(first.KeySelector)
            : query.OrderBy(first.KeySelector);

        foreach (var sort in _sorting.Skip(1))
        {
            ordered = sort.Descending ? ordered.ThenByDescending(sort.KeySelector) : ordered.ThenBy(sort.KeySelector);
        }
    }
}
