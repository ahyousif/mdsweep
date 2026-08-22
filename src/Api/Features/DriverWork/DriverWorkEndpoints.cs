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
        endpoints.MapPost("/api/driver-work/events/sync", SynchronizeEvent).RequireAuthorization();
        endpoints.MapGet("/api/driver-work/conflicts", ListConflicts).RequireAuthorization();
        endpoints.MapPost("/api/driver-work/trips/{tripNumber}/events/{eventId:guid}/corrections", CorrectEvent).RequireAuthorization();
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

        var events = await db.DriverTripEvents.Where(x => x.TripId == trip.Id)
            .OrderBy(x => x.ReceivedAt).ThenBy(x => x.Id)
            .ToListAsync(ct);
        var ids = events.Select(x => x.Id).ToArray();
        var corrections = await db.DriverTripEventCorrections.Where(x => ids.Contains(x.DriverTripEventId)).OrderBy(x => x.ReceivedAt).ToListAsync(ct);
        return Results.Ok(events.Select(x => new DriverTripEventResponse(x.Id, x.Type, x.DeviceCapturedAt, x.ReceivedAt, x.OutcomeReason, x.Note, x.TripLogSigned,
            corrections.Where(c => c.DriverTripEventId == x.Id).Select(c => new DriverTripEventCorrectionResponse(c.Id, c.DriverTripEventId, c.CorrectedDeviceCapturedAt, c.ReceivedAt, c.Reason)).ToList())));
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
                return Results.Ok(new DriverTripEventResponse(existing.Id, existing.Type, existing.DeviceCapturedAt, existing.ReceivedAt, existing.OutcomeReason, existing.Note, existing.TripLogSigned));
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
            recorded.Id, recorded.Type, recorded.DeviceCapturedAt, recorded.ReceivedAt, recorded.OutcomeReason, recorded.Note, recorded.TripLogSigned));
    }

    private static async Task<IResult> SynchronizeEvent(SynchronizeDriverTripEventRequest request, ClaimsPrincipal user, ApplicationDbContext db, IDriverWorkClock clock, CancellationToken ct)
    {
        var driver = await ResolveDriver(user, db, ct);
        if (driver is null) return Results.Forbid();
        var existingConflict = await db.DriverTripSyncConflicts.SingleOrDefaultAsync(x => x.DriverId == driver.Id && x.ActionId == request.ActionId, ct);
        if (existingConflict is not null)
            return Results.Conflict(new { message = existingConflict.Reason });
        if (await AssignedTrip(driver.Id, request.TripNumber, db, ct) is not null)
            return await RecordEvent(request.TripNumber, request.Event, user, db, clock, ct);

        var context = await ProviderContextResolver.ResolveActive(user, db, ct);
        db.DriverTripSyncConflicts.Add(new DriverTripSyncConflict
        {
            ProviderId = context!.ProviderId,
            DriverId = driver.Id,
            ActionId = request.ActionId,
            TripNumber = request.TripNumber,
            Type = request.Event.Type,
            DeviceCapturedAt = request.Event.DeviceCapturedAt,
            ReceivedAt = clock.UtcNow,
            Reason = "Trip is no longer assigned to this Driver.",
            TripLogSigned = request.Event.TripLogSigned,
            OutcomeReason = request.Event.OutcomeReason?.Trim(),
            Note = request.Event.Note?.Trim()
        });
        await db.SaveChangesAsync(ct);
        return Results.Conflict(new { message = "This queued action needs Dispatcher attention because the Trip is no longer assigned to you." });
    }

    private static async Task<IResult> ListConflicts(ClaimsPrincipal user, ApplicationDbContext db, CancellationToken ct)
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, ct);
        if (!ProviderContextResolver.HasRole(context, "Dispatcher")) return Results.Forbid();
        return Results.Ok(await db.DriverTripSyncConflicts.Where(x => x.ProviderId == context!.ProviderId)
            .OrderByDescending(x => x.ReceivedAt)
            .Select(x => new DriverTripSyncConflictResponse(x.Id, x.TripNumber, x.Type, x.DeviceCapturedAt, x.ReceivedAt, x.Reason, x.TripLogSigned, x.OutcomeReason, x.Note))
            .ToListAsync(ct));
    }

    private static async Task<IResult> CorrectEvent(string tripNumber, Guid eventId, CorrectDriverTripEventRequest request, ClaimsPrincipal user, ApplicationDbContext db, IDriverWorkClock clock, CancellationToken ct)
    {
        var driver = await ResolveDriver(user, db, ct);
        if (driver is null) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Reason)) return Results.BadRequest(new { message = "A correction reason is required." });
        if (request.DeviceCapturedAt == default) return Results.BadRequest(new { message = "Corrected device capture time is required." });
        var trip = await AssignedTrip(driver.Id, tripNumber, db, ct);
        if (trip is null) return Results.NotFound();
        var original = await db.DriverTripEvents.SingleOrDefaultAsync(x => x.Id == eventId && x.TripId == trip.Id && x.DriverId == driver.Id, ct);
        if (original is null) return Results.NotFound();
        if (original.ReceivedAt > clock.UtcNow || clock.UtcNow - original.ReceivedAt > TimeSpan.FromMinutes(15))
            return Results.BadRequest(new { message = "This event is no longer eligible for a Driver correction. Ask a Dispatcher for help." });
        var correction = new DriverTripEventCorrection { DriverTripEventId = original.Id, CorrectedByDriverId = driver.Id, CorrectedDeviceCapturedAt = request.DeviceCapturedAt, ReceivedAt = clock.UtcNow, Reason = request.Reason.Trim() };
        db.DriverTripEventCorrections.Add(correction);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/driver-work/trips/{tripNumber}/history", new DriverTripEventCorrectionResponse(correction.Id, correction.DriverTripEventId, correction.CorrectedDeviceCapturedAt, correction.ReceivedAt, correction.Reason));
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

public sealed record SynchronizeDriverTripEventRequest(Guid ActionId, string TripNumber, RecordDriverTripEventRequest Event);
public sealed record CorrectDriverTripEventRequest(DateTimeOffset DeviceCapturedAt, string Reason);
public sealed record DriverTripEventCorrectionResponse(Guid Id, Guid DriverTripEventId, DateTimeOffset CorrectedDeviceCapturedAt, DateTimeOffset ReceivedAt, string Reason);
public sealed record DriverTripSyncConflictResponse(Guid Id, string TripNumber, DriverTripEventType Type, DateTimeOffset DeviceCapturedAt, DateTimeOffset ReceivedAt, string Reason, bool? TripLogSigned, string? OutcomeReason, string? Note);

public sealed record DriverTripEventResponse(
    Guid Id,
    DriverTripEventType Type,
    DateTimeOffset DeviceCapturedAt,
    DateTimeOffset ReceivedAt,
    string? OutcomeReason,
    string? Note,
    bool? TripLogSigned,
    IReadOnlyList<DriverTripEventCorrectionResponse>? Corrections = null);

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
