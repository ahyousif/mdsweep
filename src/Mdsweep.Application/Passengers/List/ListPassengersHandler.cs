using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Passengers.Specifications;

namespace Mdsweep.Application.Passengers.List;

public sealed class ListPassengersHandler(IRepository repository)
{
    public async Task<Result<ListPassengersResult>> Handle(ListPassengersQuery query, CancellationToken ct)
    {
        var specification = new PassengersSpecification().WithSearch(query.Search).OrderByName().AsNoTracking();
        var totalCount = await repository.CountAsync(specification.Build(), ct);
        var items = await repository.ListAsync(
            specification.WithPagination(query.Page, query.PageSize).Build(PassengerListProjection.Instance),
            ct
        );
        var totalPages = (long)Math.Ceiling(totalCount / (double)query.PageSize);

        return Result.Success(new ListPassengersResult(items, totalCount, query.Page, query.PageSize, totalPages));
    }
}
