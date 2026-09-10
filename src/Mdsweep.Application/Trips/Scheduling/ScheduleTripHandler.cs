using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Tenants;
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

        if (!trip.RequiresRouteEstimate)
        {
            trip.ClearRouteEstimate();

            await repository.UpdateAsync(trip, ct);

            return;
        }

        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(trip.TenantId!, ct);

        Guard.Against.Null(tenant, $"Tenant '{trip.TenantId}' was not found for trip scheduling.");

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
            trip.ClearRouteEstimate();
        }
        else
        {
            trip.ApplyRouteEstimate(estimate.Value.Duration, estimate.Value.DistanceMeters, tenant.PickupBufferMinutes);
        }

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
