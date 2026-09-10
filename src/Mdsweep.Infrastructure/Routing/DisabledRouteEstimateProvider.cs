using Ardalis.Result;
using Mdsweep.Application.Trips.Scheduling;

namespace Mdsweep.Infrastructure.Routing;

public sealed class DisabledRouteEstimateProvider : IRouteEstimateProvider
{
    public Task<Result<RouteEstimate>> GetEstimateAsync(string origin, string destination, CancellationToken ct) =>
        Task.FromResult(Result<RouteEstimate>.Error("Route estimation is currently disabled."));
}
