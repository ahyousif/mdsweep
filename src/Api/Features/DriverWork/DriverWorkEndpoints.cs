using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Features.Dispatch;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Api.Infrastructure;

namespace Mdsweep.Api.Features.DriverWork;

public static class DriverWorkEndpoints
{
    private static readonly string[] OutcomeReasons =
    ["PassengerNoShow", "PassengerCancelled", "UnableToLocatePassenger", "VehicleIssue", "Other"];

    public static IEndpointRouteBuilder MapDriverWork(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/driver-work/trips", ListTrips).RequireAuthorization();
        endpoints.MapGet("/api/driver-work/trips/{tripNumber}/history", TripHistory).RequireAuthorization();
        endpoints.MapPost("/api/driver-work/trips/{tripNumber}/events", RecordEvent).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListTrips(ClaimsPrincipal user, ApplicationDbContext db, IDriverWorkClock clock, CancellationToken ct)
    {
        var driver = await ResolveDriver(user, db, ct);
        if (driver is null) return Results.Forbid();

        var events = db.DriverTripEvents;
        var trips = await (from assignment in db.TripAssignments
                           join trip in db.Trips on assignment.TripId equals trip.Id
                           where assignment.DriverId == driver.Id && assignment.SupersededAt == null && trip.IsActive && trip.AppointmentDate == DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)
                           orderby trip.AppointmentDate, trip.AppointmentTime, trip.TripNumber
                           select new DriverTripResponse(
                               trip.TripNumber,
                               trip.JourneyKey,
                               trip.AppointmentDate,
                               trip.AppointmentTime,
                               trip.MemberFirstName + " " + trip.MemberLastName,
                               trip.PassengerType,
                               trip.VehicleType,
                               trip.PickupAddress,
                               trip.PickupCity,
                               trip.DeliveryAddress,
                               trip.DeliveryCity,
                               trip.PassengerPhone,
                               events.Where(x => x.TripId == trip.Id).OrderByDescending(x => x.ReceivedAt).ThenByDescending(x => x.Id).Select(x => (DriverTripEventType?)x.Type).FirstOrDefault()))
            .ToListAsync(ct);

        return Results.Ok(trips.Select(x => x with { NextAction = NextAction(x.LastEventType) }));
    }

    private static async Task<IResult> TripHistory(string tripNumber, ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var driver = await ResolveDriver(user, db, ct);
        if (driver is null) return Results.Forbid();
        var trip = await AssignedTrip(driver.Id, tripNumber, db, ct);
        if (trip is null) return Results.NotFound();

        var history = await db.DriverTripEvents.Where(x => x.TripId == trip.Id)
            .OrderBy(x => x.ReceivedAt).ThenBy(x => x.Id)
            .Select(x => new DriverTripEventResponse(x.Type, x.DeviceCapturedAt, x.ReceivedAt, x.OutcomeReason, x.Note, x.TripLogSigned))
            .ToListAsync(ct);
        return Results.Ok(history);
    }

    private static async Task<IResult> RecordEvent(string tripNumber, RecordDriverTripEventRequest request, ClaimsPrincipal user, ApplicationDbContext db, IDriverWorkClock clock, CancellationToken ct)
    {
        var driver = await ResolveDriver(user, db, ct);
        if (driver is null) return Results.Forbid();
        var trip = await AssignedTrip(driver.Id, tripNumber, db, ct);
        if (trip is null) return Results.NotFound();
        if (request.DeviceCapturedAt == default) return Results.BadRequest(new { message = "Device capture time is required." });
        if (request.Type == DriverTripEventType.DroppedOff && request.TripLogSigned is null)
            return Results.BadRequest(new { message = "Record whether the Trip Log was signed before completing the Trip." });
        if (request.Type == DriverTripEventType.CouldNotComplete && !OutcomeReasons.Contains(request.OutcomeReason, StringComparer.Ordinal))
            return Results.BadRequest(new { message = "Choose a standardized Could Not Complete reason." });
        if (request.Type != DriverTripEventType.CouldNotComplete && (!string.IsNullOrWhiteSpace(request.OutcomeReason) || !string.IsNullOrWhiteSpace(request.Note)))
            return Results.BadRequest(new { message = "An outcome reason and note may only be recorded for Could Not Complete." });

        var existing = await db.DriverTripEvents.SingleOrDefaultAsync(x =>
            x.TripId == trip.Id && x.DeviceCapturedAt == request.DeviceCapturedAt, ct);
        if (existing is not null)
        {
            if (SameEvent(existing, driver.Id, request))
                return Results.Ok(new DriverTripEventResponse(existing.Type, existing.DeviceCapturedAt, existing.ReceivedAt, existing.OutcomeReason, existing.Note, existing.TripLogSigned));
            return Results.Conflict(new { message = "An event with this device capture time was already recorded with different details." });
        }

        var lastEvent = await db.DriverTripEvents.Where(x => x.TripId == trip.Id)
            .OrderByDescending(x => x.ReceivedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        var expected = NextAction(lastEvent?.Type);
        if (expected is null || (request.Type != expected && request.Type != DriverTripEventType.CouldNotComplete))
            return Results.BadRequest(new { message = expected is null
                ? "This Trip already has a physical outcome and cannot receive another event."
                : $"Record {Display(expected.Value)} before {Display(request.Type)}." });
        if (lastEvent is not null && request.DeviceCapturedAt < lastEvent.DeviceCapturedAt)
            return Results.BadRequest(new { message = "Device capture time must not be earlier than the preceding Trip event." });

        var recorded = new DriverTripEvent
        {
            TripId = trip.Id,
            DriverId = driver.Id,
            Type = request.Type,
            DeviceCapturedAt = request.DeviceCapturedAt,
            ReceivedAt = clock.UtcNow,
            OutcomeReason = request.OutcomeReason?.Trim(),
            Note = request.Note?.Trim(),
            TripLogSigned = request.TripLogSigned
        };
        db.DriverTripEvents.Add(recorded);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/driver-work/trips/{tripNumber}/history", new DriverTripEventResponse(
            recorded.Type, recorded.DeviceCapturedAt, recorded.ReceivedAt, recorded.OutcomeReason, recorded.Note, recorded.TripLogSigned));
    }

    private static async Task<Driver?> ResolveDriver(ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, ct);
        if (!ProviderContextResolver.HasRole(context, "Driver")) return null;
        return await db.Drivers.SingleOrDefaultAsync(x => x.ProviderId == context!.ProviderId && x.AppUserId == context.AppUserId && x.IsActive, ct);
    }

    private static Task<Mdsweep.Api.Features.ManifestImports.Trip?> AssignedTrip(Guid driverId, string tripNumber, ApplicationDbContext db, CancellationToken ct) =>
        (from assignment in db.TripAssignments
         join trip in db.Trips on assignment.TripId equals trip.Id
         where assignment.DriverId == driverId && assignment.SupersededAt == null && trip.TripNumber == tripNumber && trip.IsActive
         select trip).SingleOrDefaultAsync(ct);

    private static DriverTripEventType? NextAction(DriverTripEventType? lastEvent) => lastEvent switch
    {
        null => DriverTripEventType.ArrivedAtPickup,
        DriverTripEventType.ArrivedAtPickup => DriverTripEventType.PickedUp,
        DriverTripEventType.PickedUp => DriverTripEventType.ArrivedAtDropOff,
        DriverTripEventType.ArrivedAtDropOff => DriverTripEventType.DroppedOff,
        _ => null
    };

    private static string Display(DriverTripEventType type) => type switch
    {
        DriverTripEventType.ArrivedAtPickup => "Arrived at Pickup",
        DriverTripEventType.PickedUp => "Picked Up",
        DriverTripEventType.ArrivedAtDropOff => "Arrived at Drop-Off",
        DriverTripEventType.DroppedOff => "Dropped Off",
        _ => "Could Not Complete"
    };

    private static bool SameEvent(DriverTripEvent existing, Guid driverId, RecordDriverTripEventRequest request) =>
        existing.DriverId == driverId && existing.Type == request.Type && existing.TripLogSigned == request.TripLogSigned &&
        string.Equals(existing.OutcomeReason, request.OutcomeReason?.Trim(), StringComparison.Ordinal) &&
        string.Equals(existing.Note, request.Note?.Trim(), StringComparison.Ordinal);
}

public sealed record RecordDriverTripEventRequest(
    DriverTripEventType Type,
    DateTimeOffset DeviceCapturedAt,
    bool? TripLogSigned,
    string? OutcomeReason,
    string? Note);

public sealed record DriverTripEventResponse(
    DriverTripEventType Type,
    DateTimeOffset DeviceCapturedAt,
    DateTimeOffset ReceivedAt,
    string? OutcomeReason,
    string? Note,
    bool? TripLogSigned);

public sealed record DriverTripResponse(
    string TripNumber,
    string JourneyKey,
    DateOnly AppointmentDate,
    TimeOnly AppointmentTime,
    string MemberName,
    string PassengerType,
    string VehicleType,
    string PickupAddress,
    string PickupCity,
    string DeliveryAddress,
    string DeliveryCity,
    string? PassengerPhone,
    DriverTripEventType? LastEventType,
    DriverTripEventType? NextAction = null);
