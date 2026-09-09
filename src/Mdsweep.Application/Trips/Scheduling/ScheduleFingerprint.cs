using System.Security.Cryptography;
using System.Text;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Scheduling;

public static class ScheduleFingerprint
{
    private const int Version = 1;

    public static string Create(BrokerTripData data, int pickupBufferMinutes)
    {
        Guard.Against.Null(data);

        var value = string.Join(
            '\u001F',
            Version,
            data.ServiceDate,
            data.AppointmentTime,
            data.BrokerPickupTime,
            data.Direction,
            data.IsWillCall,
            data.PickupAddress,
            data.PickupCity,
            data.PickupState,
            data.PickupZip,
            data.DropoffAddress,
            data.DropoffCity,
            data.DropoffState,
            data.DropoffZip,
            pickupBufferMinutes
        );

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
