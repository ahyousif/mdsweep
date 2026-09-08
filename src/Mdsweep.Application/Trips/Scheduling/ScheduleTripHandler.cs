using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Scheduling;

public sealed class ScheduleTripHandler(IRepository repository, IRouteDurationProvider routeDurationProvider)
{
    public async Task Handle(ScheduleTripCommand message, CancellationToken ct)
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(message.TripId, ct);

        if (trip is null)
        {
            return;
        }

        if (trip.BrokerData.Direction == TripDirection.From)
        {
            var brokerPickupTime = trip.BrokerData.IsWillCall ? null : trip.BrokerData.Time;

            trip.UpdateCalculatedSchedule(brokerPickupTime, scheduleInputFingerprint: null);

            await repository.UpdateAsync(trip, ct);

            return;
        }

        var fingerprint = ScheduleFingerprint.Create(trip.BrokerData);

        if (trip.ScheduleInputFingerprint == fingerprint)
        {
            return;
        }

        if (trip.BrokerData.Time is null)
        {
            trip.UpdateCalculatedSchedule(pickupTime: null, scheduleInputFingerprint: null);

            await repository.UpdateAsync(trip, ct);

            return;
        }

        var origin = FormatAddress(
            trip.BrokerData.PickupAddress,
            trip.BrokerData.PickupCity,
            trip.BrokerData.PickupState,
            trip.BrokerData.PickupZip
        );

        var destination = FormatAddress(
            trip.BrokerData.DropoffAddress,
            trip.BrokerData.DropoffCity,
            trip.BrokerData.DropoffState,
            trip.BrokerData.DropoffZip
        );

        var duration = await routeDurationProvider.GetDurationAsync(origin, destination, ct);

        if (!duration.IsSuccess)
        {
            // Never leave an old calculated time behind after
            // the scheduling inputs changed.
            trip.UpdateCalculatedSchedule(pickupTime: null, scheduleInputFingerprint: null);

            await repository.UpdateAsync(trip, ct);

            return;
        }

        var calculatedPickupTime = TripSchedulingPolicy.CalculatePickupTime(trip.BrokerData.Time.Value, duration.Value);

        trip.UpdateCalculatedSchedule(calculatedPickupTime, fingerprint);

        await repository.UpdateAsync(trip, ct);
    }

    private static string FormatAddress(string address, string city, string? state, string? zip)
    {
        var parts = new List<string> { address, city };

        if (!string.IsNullOrWhiteSpace(state))
        {
            parts.Add(state);
        }

        if (!string.IsNullOrWhiteSpace(zip))
        {
            parts.Add(zip);
        }

        return string.Join(", ", parts);
    }
}
