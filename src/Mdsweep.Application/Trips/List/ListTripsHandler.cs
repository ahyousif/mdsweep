using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Trips.Specifications;

namespace Mdsweep.Application.Trips.List;

public sealed class ListTripsHandler(IRepository repository)
{
    public async Task<Result<ListTripsResult>> Handle(ListTripsQuery query, CancellationToken ct)
    {
        var trips = new TripsSpecification().WithTripDateRange(query.StartDate, query.EndDate);

        var count = await repository.CountAsync(trips.Build(), ct);

        var items = await repository.ListAsync(
            trips
                .OrderBy(query.SortBy, query.SortDirection, query.StartDate != query.EndDate)
                .WithPagination(query.Page, query.PageSize)
                .Build(TripModelProjection.Instance),
            ct
        );

        var totalPages = (long)Math.Ceiling(count / (double)query.PageSize);

        return new ListTripsResult(items, count, query.Page, query.PageSize, totalPages, count, 0);
    }
}
