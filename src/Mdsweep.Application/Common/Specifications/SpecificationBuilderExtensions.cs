using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Application.Common.Specifications;

public static class SpecificationBuilderExtensions
{
    public static TSelf WithId<TEntity, TId, TSelf>(this SpecificationBuilder<TEntity, TId, TSelf> builder, TId id)
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        builder.Spec.Add(query => query.Where(entity => entity.Id.Equals(id)));

        return (TSelf)builder;
    }

    public static TSelf WithIds<TEntity, TId, TSelf>(
        this SpecificationBuilder<TEntity, TId, TSelf> builder,
        IEnumerable<TId> ids
    )
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        var idValues = ids.ToArray();

        builder.Spec.Add(query => query.Where(entity => idValues.Contains(entity.Id)));

        return (TSelf)builder;
    }

    public static TSelf WithoutId<TEntity, TId, TSelf>(this SpecificationBuilder<TEntity, TId, TSelf> builder, TId id)
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        builder.Spec.Add(query => query.Where(entity => !entity.Id.Equals(id)));

        return (TSelf)builder;
    }

    public static TSelf OrderBy<TEntity, TId, TSelf>(
        this SpecificationBuilder<TEntity, TId, TSelf> builder,
        Expression<Func<TEntity, object?>> keySelector,
        bool descending = false
    )
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        builder.Spec.AddSorting(keySelector, descending);

        return (TSelf)builder;
    }

    public static TSelf Include<TEntity, TId, TSelf, TProperty>(
        this SpecificationBuilder<TEntity, TId, TSelf> builder,
        Expression<Func<TEntity, TProperty>> navigationSelector
    )
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        builder.Spec.Add(query => query.Include(navigationSelector));

        return (TSelf)builder;
    }

    public static TSelf AsNoTracking<TEntity, TId, TSelf>(this SpecificationBuilder<TEntity, TId, TSelf> builder)
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        builder.Spec.Add(query => query.AsNoTracking());

        return (TSelf)builder;
    }

    public static TSelf UseSplitQuery<TEntity, TId, TSelf>(
        this SpecificationBuilder<TEntity, TId, TSelf> builder,
        bool enable = true
    )
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        builder.Spec.Add(query => query.AsSplitQuery(enable));

        return (TSelf)builder;
    }

    public static TSelf WithPagination<TEntity, TId, TSelf>(
        this SpecificationBuilder<TEntity, TId, TSelf> builder,
        int page,
        int pageSize
    )
        where TEntity : Entity<TId>
        where TId : notnull
        where TSelf : SpecificationBuilder<TEntity, TId, TSelf>
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        builder.Spec.Add(query => query.Skip((page - 1) * pageSize).Take(pageSize));

        return (TSelf)builder;
    }
}
