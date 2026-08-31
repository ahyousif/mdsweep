using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.DriverWork;

namespace Mdsweep.Api.Features.DriverWork;

public static class DriverWorkEndpoints
{
    [WolverineGet("/api/driver-work/trips")]
    public static async Task<IResult> ListTrips(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<IReadOnlyList<DriverTripResponse>>>(
            new ListDriverTrips(context.TenantId, context.UserId),
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
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<
            DriverWorkResult<IReadOnlyList<DriverTripEventResponse>>
        >(
            new GetDriverTripHistory(context.TenantId, context.UserId, tripNumber),
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
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<DriverTripEventResponse>>(
            new RecordDriverTripEvent(context.TenantId, context.UserId, tripNumber, request),
            cancellationToken
        );
        return EventResult(result);
    }

    [WolverinePost("/api/driver-work/events/sync")]
    public static async Task<IResult> SynchronizeEvent(
        SynchronizeDriverTripEventRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<DriverTripEventResponse>>(
            new SynchronizeDriverTripEvent(context.TenantId, context.UserId, request),
            cancellationToken
        );
        return EventResult(result);
    }

    [WolverineGet("/api/driver-work/conflicts")]
    public static async Task<IResult> ListConflicts(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Dispatcher", tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var conflicts = await bus.InvokeAsync<List<DriverTripSyncConflictResponse>>(
            new ListDriverSyncConflicts(context.TenantId),
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
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await Resolve(user, "Driver", tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();

        var result = await bus.InvokeAsync<DriverWorkResult<DriverTripEventCorrectionResponse>>(
            new CorrectDriverTripEvent(
                context.TenantId,
                context.UserId,
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

    private static async Task<TenantContext?> Resolve(
        ClaimsPrincipal user,
        string role,
        ITenantAccess tenantAccess,
        CancellationToken cancellationToken
    )
    {
        var context = await TenantContextResolver.ResolveActive(user, tenantAccess, cancellationToken);
        return TenantContextResolver.HasRole(context, role) ? context : null;
    }
}
