using Ardalis.Result;
using Mdsweep.Application.Trips.Scheduling;

namespace Mdsweep.Infrastructure.Routing;

public sealed class DisabledRouteDurationProvider : IRouteDurationProvider
{
    public Task<Result<Duration>> GetDurationAsync(string origin, string destination, CancellationToken ct) =>
        Task.FromResult(Result<Duration>.Error("Route estimation is currently disabled."));
}
