using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Infrastructure;
using Mdsweep.Api.Features.Identity;

namespace Mdsweep.Api.Features.Dispatch;

public static class DispatchEndpoints
{
    public static IEndpointRouteBuilder MapDispatch(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/trips/{tripNumber}/scheduled-pickup-time", SetScheduledPickupTime)
            .RequireAuthorization(policy => policy.RequireRole("Dispatcher"));
        endpoints.MapGet("/api/trips/{tripNumber}/scheduled-pickup-time/history", GetScheduledPickupTimeHistory)
            .RequireAuthorization(policy => policy.RequireRole("Dispatcher"));
        endpoints.MapGet("/api/service-days/{serviceDate}/trips", GetServiceDay)
            .RequireAuthorization(policy => policy.RequireRole("Dispatcher"));
        return endpoints;
    }

    private static async Task<IResult> SetScheduledPickupTime(
        string tripNumber,
        SetScheduledPickupTimeRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, cancellationToken);
        if (context is null) return Results.Forbid();

        var trip = await db.Trips.SingleOrDefaultAsync(x => x.ProviderId == context.ProviderId && x.TripNumber == tripNumber, cancellationToken);
        if (trip is null) return Results.NotFound();
        if (!trip.IsActive)
            return Results.BadRequest(new { message = "An inactive Trip cannot be scheduled." });

        var schedule = await db.TripSchedules.FindAsync([trip.Id], cancellationToken);
        if (schedule?.ScheduledPickupTime == request.ScheduledPickupTime)
            return Results.Ok(new { request.ScheduledPickupTime });

        if (schedule is null)
        {
            schedule = new TripSchedule { TripId = trip.Id, ScheduledPickupTime = request.ScheduledPickupTime };
            db.TripSchedules.Add(schedule);
        }
        else
        {
            schedule.ScheduledPickupTime = request.ScheduledPickupTime;
        }

        db.ScheduledPickupTimeChanges.Add(new ScheduledPickupTimeChange
        {
            TripId = trip.Id,
            ScheduledPickupTime = request.ScheduledPickupTime,
            ChangedBy = context.AppUserId.ToString()
        });
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { request.ScheduledPickupTime });
    }

    private static async Task<IResult> GetScheduledPickupTimeHistory(
        string tripNumber,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, cancellationToken);
        if (context is null) return Results.Forbid();
        var tripId = await db.Trips.Where(x => x.ProviderId == context.ProviderId && x.TripNumber == tripNumber)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (!tripId.HasValue) return Results.NotFound();

        var changes = await db.ScheduledPickupTimeChanges
            .Where(x => x.TripId == tripId.Value)
            .OrderBy(x => x.Sequence)
            .Select(x => new ScheduledPickupTimeChangeResponse(x.Sequence, x.ScheduledPickupTime, x.ChangedAt, x.ChangedBy))
            .ToListAsync(cancellationToken);
        return Results.Ok(changes);
    }

    private static async Task<IResult> GetServiceDay(
        DateOnly serviceDate,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, cancellationToken);
        if (context is null) return Results.Forbid();
        var trips = await db.Trips.Where(x => x.ProviderId == context.ProviderId && x.AppointmentDate == serviceDate)
            .OrderBy(x => x.AppointmentTime)
            .Select(x => new ServiceDayTripResponse(
                x.TripNumber, x.JourneyKey, x.MemberFirstName + " " + x.MemberLastName,
                x.PickupAddress, x.PickupCity, x.DeliveryAddress, x.DeliveryCity,
                x.PassengerType, x.VehicleType, x.BrokerStatus, x.AppointmentTime,
                db.TripSchedules.Where(schedule => schedule.TripId == x.Id)
                    .Select(schedule => (TimeOnly?)schedule.ScheduledPickupTime).SingleOrDefault(),
                x.IsWillCall, x.IsActive))
            .ToListAsync(cancellationToken);
        return Results.Ok(trips);
    }
}
