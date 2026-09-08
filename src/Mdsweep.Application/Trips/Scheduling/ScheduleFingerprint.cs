using System.Security.Cryptography;
using System.Text;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Scheduling;

public static class ScheduleFingerprint
{
    public static string Create(BrokerTripData data)
    {
        Guard.Against.Null(data);

        var value = string.Join(
            '\u001F',
            TripSchedulingPolicy.Version,
            data.ServiceDate,
            data.Time,
            data.Direction,
            data.IsWillCall,
            data.PickupAddress,
            data.PickupCity,
            data.PickupState,
            data.PickupZip,
            data.DropoffAddress,
            data.DropoffCity,
            data.DropoffState,
            data.DropoffZip
        );

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
