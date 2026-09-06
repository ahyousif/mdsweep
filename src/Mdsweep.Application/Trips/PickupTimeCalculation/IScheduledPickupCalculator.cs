namespace Mdsweep.Application.Trips.PickupTimeCalculation;

public interface IScheduledPickupCalculator
{
    string PolicyFingerprint { get; }
    LocalTime? Calculate(LocalTime appointmentTime, TimeSpan estimatedDuration);
}
