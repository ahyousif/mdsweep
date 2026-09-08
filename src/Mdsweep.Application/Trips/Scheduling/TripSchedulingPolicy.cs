namespace Mdsweep.Application.Trips.Scheduling;

internal static class TripSchedulingPolicy
{
    public const int PickupBufferMinutes = 15;

    public const int Version = 1;

    public static LocalTime CalculatePickupTime(LocalTime appointmentTime, Duration travelDuration)
    {
        var travelMinutes = (int)Math.Ceiling(travelDuration.TotalMinutes);

        return appointmentTime.PlusMinutes(-(travelMinutes + PickupBufferMinutes));
    }
}
