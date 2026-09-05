namespace Mdsweep.Application.Trips.Scheduling;

public interface IScheduledPickupCalculator
{
    string PolicyFingerprint { get; }
    LocalTime? Calculate(LocalTime appointmentTime, TimeSpan estimatedDuration);
}
