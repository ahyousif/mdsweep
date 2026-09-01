using Mdsweep.Application.Common.Pagination;
using Mdsweep.Application.Common.Persistence;
using ApplicationPagedResult = Mdsweep.Application.Common.Pagination.PagedResult<Mdsweep.Application.Trips.TripModel>;

namespace Mdsweep.Application.Trips.List;

public sealed class ListTripsHandler(IRepository repository)
{
    public async Task<Result<ApplicationPagedResult>> Handle(ListTripsQuery query, CancellationToken ct)
    {
        var totalCount = await repository.CountAsync(new CountTripsSpecification(query), ct);
        var items = await repository.ListAsync(new ListTripsSpecification(query), ct);

        return Result.Success(new ApplicationPagedResult(items, totalCount, query.Page, query.PageSize));
    }
}
