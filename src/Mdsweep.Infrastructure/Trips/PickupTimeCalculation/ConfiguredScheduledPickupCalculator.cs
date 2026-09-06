using Mdsweep.Application.Trips.PickupTimeCalculation;
using Mdsweep.Domain.Trips;
using Microsoft.Extensions.Options;

namespace Mdsweep.Infrastructure.Trips.PickupTimeCalculation;

public sealed class ConfiguredScheduledPickupCalculator(IOptions<PickupTimeCalculationOptions> options)
    : IScheduledPickupCalculator
{
    private readonly PickupTimeCalculationOptions _options = options.Value;

    public string PolicyFingerprint => $"{_options.PolicyVersion}:{_options.PickupTimeBufferMinutes}";

    public LocalTime? Calculate(LocalTime appointmentTime, TimeSpan estimatedDuration) =>
        ScheduledPickupCalculationPolicy.Calculate(
            appointmentTime,
            estimatedDuration,
            _options.PickupTimeBufferMinutes
        );
}
