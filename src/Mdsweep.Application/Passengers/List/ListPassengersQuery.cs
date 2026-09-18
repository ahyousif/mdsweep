using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Passengers.List;

public sealed record ListPassengersQuery(string? Search = null, int Page = 1, int PageSize = 50)
    : IQuery<ListPassengersResult>;
