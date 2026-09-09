namespace Mdsweep.Application.Trips.Scheduling;

internal static class TripSchedulingPolicy
{
    public const int PickupBufferMinutes = 15;

    public const int Version = 1;

    public static LocalTime CalculatePickupTime(LocalTime appointmentTime, Duration travelDuration)
    {
        var travelMinutes = CalculateTravelMinutes(travelDuration);

        return appointmentTime.PlusMinutes(-(travelMinutes + PickupBufferMinutes));
    }

    public static int CalculateTravelMinutes(Duration travelDuration) => (int)Math.Ceiling(travelDuration.TotalMinutes);
}
