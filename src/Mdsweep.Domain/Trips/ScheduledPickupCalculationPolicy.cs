namespace Mdsweep.Domain.Trips;

public static class ScheduledPickupCalculationPolicy
{
    public static LocalTime Calculate(LocalTime appointmentTime, TimeSpan estimatedDuration, int bufferMinutes)
    {
        Guard.Against.NegativeOrZero(bufferMinutes, nameof(bufferMinutes));
        Guard.Against.Negative(estimatedDuration, nameof(estimatedDuration));

        var raw = TimeSpan.FromTicks(appointmentTime.TickOfDay) - estimatedDuration - TimeSpan.FromMinutes(bufferMinutes);
        var roundedEarlierMinutes = Math.Max(0, (int)Math.Floor(raw.TotalMinutes / 5) * 5);
        return LocalTime.FromMinutesSinceMidnight(roundedEarlierMinutes);
    }
}
