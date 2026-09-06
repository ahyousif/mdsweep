namespace Mdsweep.Domain.Trips;

public static class ScheduledPickupCalculationPolicy
{
    public static LocalTime? Calculate(LocalTime appointmentTime, TimeSpan estimatedDuration, int bufferMinutes)
    {
        Guard.Against.NegativeOrZero(bufferMinutes, nameof(bufferMinutes));
        Guard.Against.Negative(estimatedDuration, nameof(estimatedDuration));

        var raw = TimeSpan.FromTicks(appointmentTime.TickOfDay) - estimatedDuration - TimeSpan.FromMinutes(bufferMinutes);
        // ScheduledPickupTime is a LocalTime. A prior-day result cannot be represented safely,
        // so the dispatcher must set it manually rather than receiving a fabricated midnight time.
        if (raw < TimeSpan.Zero) return null;
        var roundedEarlierMinutes = (int)Math.Floor(raw.TotalMinutes / 5) * 5;
        return LocalTime.FromMinutesSinceMidnight(roundedEarlierMinutes);
    }
}
