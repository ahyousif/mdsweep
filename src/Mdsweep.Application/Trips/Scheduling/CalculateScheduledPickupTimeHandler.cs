using System.Security.Cryptography;
using System.Text;
using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Trips;
using Microsoft.Extensions.Logging;

namespace Mdsweep.Application.Trips.Scheduling;

public sealed class CalculateScheduledPickupTimeHandler(
    IRepository repository,
    IRouteEstimator routeEstimator,
    IScheduledPickupCalculator calculator,
    ILogger<CalculateScheduledPickupTimeHandler> logger
)
{
    public async Task<Result<Guid>> Handle(CalculateScheduledPickupTimeCommand command, CancellationToken ct)
    {
        var trip = await repository.GetByIdAsync<TripAggregate, Guid>(command.TripId, ct);
        if (trip is null)
        {
            return Result.NotFound();
        }

        if (trip.BrokerData.IsWillCall || trip.BrokerData.AppointmentTime is null)
        {
            return Result.Success(trip.Id);
        }

        var fingerprint = CreateFingerprint(trip.BrokerData, calculator.PolicyFingerprint);
        if (trip.SchedulingInputFingerprint == fingerprint)
        {
            return Result.Success(trip.Id);
        }

        try
        {
            var duration = await routeEstimator.EstimateDurationAsync(
                new RouteLocation(trip.BrokerData.PickupAddress, trip.BrokerData.PickupCity),
                new RouteLocation(trip.BrokerData.DropoffAddress, trip.BrokerData.DropoffCity),
                ct
            );
            if (duration is null)
            {
                logger.LogWarning("Route estimation was unavailable for trip {TripId}", trip.Id);
                return Result.Success(trip.Id);
            }

            var minutes = (int)Math.Ceiling(duration.Value.TotalMinutes);
            var suggestion = calculator.Calculate(trip.BrokerData.AppointmentTime.Value, TimeSpan.FromMinutes(minutes));
            trip.ApplyCalculatedPickupTime(suggestion, minutes, fingerprint);
            await repository.UpdateAsync(trip, ct);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Route estimation failed for trip {TripId}", trip.Id);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Route estimation timed out for trip {TripId}", trip.Id);
        }

        return Result.Success(trip.Id);
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
            policyFingerprint
        );
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
