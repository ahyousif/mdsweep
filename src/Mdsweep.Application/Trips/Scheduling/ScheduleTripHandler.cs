using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.Trips.Scheduling;

public sealed class ScheduleTripHandler(IRepository repository, IRouteEstimateProvider routeEstimateProvider)
{
    public async Task Handle(ScheduleTripCommand command, CancellationToken ct)
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(command.TripId, ct);

        if (trip is null)
        {
            return;
        }

        if (trip.BrokerData.Direction == TripDirection.From)
        {
            var brokerPickupTime = trip.BrokerData.IsWillCall ? null : trip.BrokerData.Time;

            if (trip.CalculatedPickupTime == brokerPickupTime && trip.ScheduleInputFingerprint is null)
            {
                return;
            }

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

        var estimate = await routeEstimateProvider.GetEstimateAsync(origin, destination, ct);

        if (!estimate.IsSuccess)
        {
            // Never leave an old calculated time behind after
            // the scheduling inputs changed.
            trip.UpdateCalculatedSchedule(pickupTime: null, scheduleInputFingerprint: null);

            await repository.UpdateAsync(trip, ct);

            return;
        }

        var calculatedPickupTime = TripSchedulingPolicy.CalculatePickupTime(
            trip.BrokerData.Time.Value,
            estimate.Value.Duration
        );

        var travelMinutes = TripSchedulingPolicy.CalculateTravelMinutes(estimate.Value.Duration);

        trip.UpdateCalculatedSchedule(calculatedPickupTime, fingerprint, travelMinutes, estimate.Value.DistanceMeters);

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
