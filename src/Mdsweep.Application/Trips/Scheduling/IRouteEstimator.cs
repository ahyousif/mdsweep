namespace Mdsweep.Application.Trips.Scheduling;

public sealed record RouteLocation(string Address, string City);

public interface IRouteEstimator
{
    Task<TimeSpan?> EstimateDurationAsync(RouteLocation pickup, RouteLocation dropoff, CancellationToken ct);
}
