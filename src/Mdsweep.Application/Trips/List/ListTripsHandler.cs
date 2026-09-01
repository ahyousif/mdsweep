using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Trips.Specifications;

namespace Mdsweep.Application.Trips.List;

public sealed class ListTripsHandler(IRepository repository)
{
    public async Task<PagedResult<IReadOnlyList<TripModel>>> Handle(ListTripsQuery query, CancellationToken ct)
    {
        var trips = new TripsSpecification()
            .WithServiceDate(query.ServiceDate)
            .WithBrokerStatus(query.BrokerStatus)
            .WithWillCall(query.IsWillCall);

        var totalCount = await repository.CountAsync(trips.Build(), ct);

        var items = await repository.ListAsync(
            trips
                .OrderBy(query.SortBy, query.SortDirection)
                .WithPagination(query.Page, query.PageSize)
                .Build(TripModelProjection.Instance),
            ct
        );

        var totalPages = (long)Math.Ceiling(totalCount / (double)query.PageSize);

        var pagedInfo = new PagedInfo(query.Page, query.PageSize, totalPages, totalCount);

        return new PagedResult<IReadOnlyList<TripModel>>(pagedInfo, items);
    }
}
