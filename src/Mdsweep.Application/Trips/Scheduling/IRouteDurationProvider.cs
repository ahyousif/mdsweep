namespace Mdsweep.Application.Trips.Scheduling;

public interface IRouteDurationProvider
{
    Task<Result<Duration>> GetDurationAsync(string origin, string destination, CancellationToken ct);
}
