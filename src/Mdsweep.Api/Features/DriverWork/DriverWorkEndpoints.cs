using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.DriverWork;
using Mdsweep.Infrastructure.Persistence;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.DriverWork;

public static class DriverWorkEndpoints
{
    [WolverineGet("/api/driver-work/trips")]
    public static async Task<IResult> ListTrips(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", db, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<IReadOnlyList<DriverTripResponse>>>(
            new ListDriverTrips(context.ProviderId, context.AppUserId),
            cancellationToken
        );
        return result.Outcome == DriverWorkOutcome.Success
            ? Results.Ok(result.Value)
            : Results.Forbid();
    }

    [WolverineGet("/api/driver-work/trips/{tripNumber}/history")]
    public static async Task<IResult> TripHistory(
        string tripNumber,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", db, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<
            DriverWorkResult<IReadOnlyList<DriverTripEventResponse>>
        >(
            new GetDriverTripHistory(context.ProviderId, context.AppUserId, tripNumber),
            cancellationToken
        );
        return result.Outcome switch
        {
            DriverWorkOutcome.Success => Results.Ok(result.Value),
            DriverWorkOutcome.Forbid => Results.Forbid(),
            _ => Results.NotFound(),
        };
    }

    [WolverinePost("/api/driver-work/trips/{tripNumber}/events")]
    public static async Task<IResult> RecordEvent(
        string tripNumber,
        RecordDriverTripEventRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", db, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<DriverTripEventResponse>>(
            new RecordDriverTripEvent(context.ProviderId, context.AppUserId, tripNumber, request),
            cancellationToken
        );
        return EventResult(result);
    }

    [WolverinePost("/api/driver-work/events/sync")]
    public static async Task<IResult> SynchronizeEvent(
        SynchronizeDriverTripEventRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", db, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<DriverTripEventResponse>>(
            new SynchronizeDriverTripEvent(context.ProviderId, context.AppUserId, request),
            cancellationToken
        );
        return EventResult(result);
    }

    [WolverineGet("/api/driver-work/conflicts")]
    public static async Task<IResult> ListConflicts(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Dispatcher", db, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var conflicts = await bus.InvokeAsync<List<DriverTripSyncConflictResponse>>(
            new ListDriverSyncConflicts(context.ProviderId),
            cancellationToken
        );
        return Results.Ok(conflicts);
    }

    [WolverinePost("/api/driver-work/trips/{tripNumber}/events/{eventId:guid}/corrections")]
    public static async Task<IResult> CorrectEvent(
        string tripNumber,
        Guid eventId,
        CorrectDriverTripEventRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", db, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<DriverTripEventCorrectionResponse>>(
            new CorrectDriverTripEvent(
                context.ProviderId,
                context.AppUserId,
                tripNumber,
                eventId,
                request
            ),
            cancellationToken
        );
        return result.Outcome switch
        {
            DriverWorkOutcome.Success => Results.Created(result.Location!, result.Value),
            DriverWorkOutcome.Forbid => Results.Forbid(),
            DriverWorkOutcome.NotFound => Results.NotFound(),
            _ => Results.BadRequest(new { message = result.Message }),
        };
    }

    private static IResult EventResult(DriverWorkResult<DriverTripEventResponse> result) =>
        result.Outcome switch
        {
            DriverWorkOutcome.Success when result.Location is not null => Results.Created(
                result.Location,
                result.Value
            ),
            DriverWorkOutcome.Success => Results.Ok(result.Value),
            DriverWorkOutcome.Forbid => Results.Forbid(),
            DriverWorkOutcome.NotFound => Results.NotFound(),
            DriverWorkOutcome.Conflict => Results.Conflict(new { message = result.Message }),
            _ => Results.BadRequest(new { message = result.Message }),
        };

    private static async Task<ProviderContext?> Resolve(
        ClaimsPrincipal user,
        string role,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, cancellationToken);
        return ProviderContextResolver.HasRole(context, role) ? context : null;
    }
}
