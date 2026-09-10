namespace Mdsweep.Application.Trips.Scheduling;

public interface IRouteEstimateProvider
{
    Task<Result<RouteEstimate>> GetEstimateAsync(string origin, string destination, CancellationToken ct);
}
