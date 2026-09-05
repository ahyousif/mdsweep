using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Domain.Trips;
using Microsoft.Extensions.Options;

namespace Mdsweep.Infrastructure.Trips.Scheduling;

public sealed class ConfiguredScheduledPickupCalculator(IOptions<TripSchedulingOptions> options)
    : IScheduledPickupCalculator
{
    private readonly TripSchedulingOptions _options = options.Value;

    public string PolicyFingerprint => $"{_options.PolicyVersion}:{_options.SchedulingBufferMinutes}";

    public LocalTime Calculate(LocalTime appointmentTime, TimeSpan estimatedDuration) =>
        ScheduledPickupCalculationPolicy.Calculate(
            appointmentTime,
            estimatedDuration,
            _options.SchedulingBufferMinutes
        );
}
