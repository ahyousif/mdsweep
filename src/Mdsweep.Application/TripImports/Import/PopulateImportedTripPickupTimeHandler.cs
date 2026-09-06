using System.Security.Cryptography;
using System.Text;
using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Trips.PickupTimeCalculation;
using Mdsweep.Application.Trips.Routing;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.TripImports.Import;

public sealed class PopulateImportedTripPickupTimeHandler(
    IRepository repository,
    IRouteEstimator routeEstimator,
    IScheduledPickupCalculator calculator
)
{
    public async Task Handle(PopulateImportedTripPickupTime message, CancellationToken ct)
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(message.TripId, ct);
        if (trip is null)
        {
            return;
        }

        if (trip.BrokerData.IsWillCall || trip.BrokerData.AppointmentTime is null)
        {
            trip.ApplyScheduledPickupTime(null, null, CreateFingerprint(trip.BrokerData, calculator.PolicyFingerprint));
            await repository.UpdateAsync(trip, ct);
            return;
        }

        var fingerprint = CreateFingerprint(trip.BrokerData, calculator.PolicyFingerprint);
        if (trip.SchedulingInputFingerprint == fingerprint)
        {
            return;
        }

        var duration = await routeEstimator.EstimateDurationAsync(
            new RouteLocation(trip.BrokerData.PickupAddress, trip.BrokerData.PickupCity),
            new RouteLocation(trip.BrokerData.DropoffAddress, trip.BrokerData.DropoffCity),
            ct
        );
        if (duration is null)
        {
            return;
        }

        var minutes = (int)Math.Ceiling(duration.Value.TotalMinutes);
        var scheduledPickupTime = calculator.Calculate(trip.BrokerData.AppointmentTime.Value, TimeSpan.FromMinutes(minutes));
        trip.ApplyScheduledPickupTime(scheduledPickupTime, minutes, fingerprint);
        await repository.UpdateAsync(trip, ct);
    }

    private static string CreateFingerprint(BrokerTripData data, string policyFingerprint)
    {
        var value = string.Join(
            '\u001f',
            data.ServiceDate,
            data.AppointmentTime,
            data.PickupAddress,
            data.PickupCity,
            data.DropoffAddress,
            data.DropoffCity,
            data.IsWillCall,
            policyFingerprint
        );
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
