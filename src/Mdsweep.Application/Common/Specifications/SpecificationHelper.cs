namespace Mdsweep.Application.Common.Specifications;

public sealed class SpecificationHelper<TEntity>
    where TEntity : class
{
    private readonly List<Action<ISpecificationBuilder<TEntity>>> _steps = [];

    public void Add(Action<ISpecificationBuilder<TEntity>> step)
    {
        _steps.Add(step);
    }

    public Specification<TEntity> Build()
    {
        var specification = new Specification<TEntity>();

        foreach (var step in _steps)
        {
            step(specification.Query);
        }

        return specification;
    }

    public Specification<TEntity, TResult> Build<TResult>(Specification<TEntity, TResult> projection)
    {
        return Build().WithProjectionOf(projection);
    }
}
