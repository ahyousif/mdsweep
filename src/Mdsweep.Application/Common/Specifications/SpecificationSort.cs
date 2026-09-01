namespace Mdsweep.Application.Common.Specifications;

internal sealed record SpecificationSort<TEntity>(Expression<Func<TEntity, object?>> KeySelector, bool Descending);
