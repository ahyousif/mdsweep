namespace Mdsweep.Application.Trips.Scheduling;

internal static class TripSchedulingPolicy
{
    public const int Version = 1;

    public static LocalTime CalculatePickupTime(LocalTime appointmentTime, Duration travelDuration, int pickupBufferMinutes)
    {
        var travelMinutes = CalculateTravelMinutes(travelDuration);

        return appointmentTime.PlusMinutes(-(travelMinutes + pickupBufferMinutes));
    }

    public static int CalculateTravelMinutes(Duration travelDuration) => (int)Math.Ceiling(travelDuration.TotalMinutes);
}
