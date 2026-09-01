using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.List;

public sealed class ListTripsHandler(IRepository repository)
{
    public async Task<PagedResult<IReadOnlyList<TripModel>>> Handle(ListTripsQuery query, CancellationToken ct)
    {
        var totalCount = await repository.CountAsync(new CountTripsSpecification(query), ct);

        var items = await repository.ListAsync(new ListTripsSpecification(query), ct);

        var totalPages = (long)Math.Ceiling(totalCount / (double)query.PageSize);

        var pagedInfo = new PagedInfo(query.Page, query.PageSize, totalPages, totalCount);

        return new PagedResult<IReadOnlyList<TripModel>>(pagedInfo, items);
    }
}
