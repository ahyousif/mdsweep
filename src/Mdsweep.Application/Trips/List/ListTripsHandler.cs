using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Trips.Specifications;

namespace Mdsweep.Application.Trips.List;

public sealed class ListTripsHandler(IRepository repository)
{
    public async Task<Result<ListTripsResult>> Handle(ListTripsQuery query, CancellationToken ct)
    {
        var trips = new TripsSpecification()
            .WithTripDateRange(query.StartDate, query.EndDate)
            .WithSearch(query.Search)
            .WithBrokerStatus(query.BrokerStatus)
            .WithWillCall(query.IsWillCall);

        var scopeCount = await repository.CountAsync(trips.Build(), ct);
        var attentionCount = await repository.CountAsync(
            new TripsSpecification()
                .WithTripDateRange(query.StartDate, query.EndDate)
                .WithSearch(query.Search)
                .WithBrokerStatus(query.BrokerStatus)
                .WithWillCall(query.IsWillCall)
                .WithNeedsAttention(true)
                .Build(),
            ct
        );
        var totalCount = query.NeedsAttention switch
        {
            true => attentionCount,
            false => scopeCount - attentionCount,
            _ => scopeCount,
        };
        trips.WithNeedsAttention(query.NeedsAttention);

        var items = await repository.ListAsync(
            trips
                .OrderBy(query.SortBy, query.SortDirection, query.StartDate != query.EndDate)
                .WithPagination(query.Page, query.PageSize)
                .Build(TripModelProjection.Instance),
            ct
        );

        var totalPages = (long)Math.Ceiling(totalCount / (double)query.PageSize);

        return new ListTripsResult(
            items,
            totalCount,
            query.Page,
            query.PageSize,
            totalPages,
            scopeCount,
            attentionCount
        );
    }
}
