using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;
using Mdsweep.Infrastructure.Persistence;
using Wolverine;
using Wolverine.Http;

namespace Mdsweep.Api.Features.Dispatch;

public static class DispatchManagementEndpoints
{
    [WolverineGet("/api/drivers")]
    public static async Task<IResult> ListDrivers(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        return Results.Ok(
            await bus.InvokeAsync<List<DriverResponse>>(
                new ListDrivers(context.ProviderId),
                cancellationToken
            )
        );
    }

    [WolverinePost("/api/drivers")]
    public static async Task<IResult> CreateDriver(
        CreateDriverRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<DriverResponse>>(
            new CreateDriver(context.ProviderId, request),
            cancellationToken
        );
        return CreatedResult(result);
    }

    [WolverinePost("/api/drivers/access")]
    public static async Task<IResult> CreateDriverAccess(
        CreateDriverAccessRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<DriverResponse>>(
            new CreateDriverAccess(context.ProviderId, request),
            cancellationToken
        );
        return CreatedResult(result);
    }

    [WolverinePost("/api/drivers/{driverId:guid}/reset-access")]
    public static async Task<IResult> ResetDriverAccess(
        Guid driverId,
        ResetDriverAccessRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new ResetDriverAccess(context.ProviderId, driverId, request),
            cancellationToken
        );
        return NoContentResult(result);
    }

    [WolverinePost("/api/drivers/{driverId:guid}/deactivate")]
    public static async Task<IResult> DeactivateDriver(
        Guid driverId,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new DeactivateDriver(context.ProviderId, driverId),
            cancellationToken
        );
        return NoContentResult(result);
    }

    [WolverineGet("/api/vehicles")]
    public static async Task<IResult> ListVehicles(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        return Results.Ok(
            await bus.InvokeAsync<List<VehicleResponse>>(
                new ListVehicles(context.ProviderId),
                cancellationToken
            )
        );
    }

    [WolverinePost("/api/vehicles")]
    public static async Task<IResult> CreateVehicle(
        CreateVehicleRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<VehicleResponse>>(
            new CreateVehicle(context.ProviderId, request),
            cancellationToken
        );
        return CreatedResult(result);
    }

    [WolverinePost("/api/vehicles/{vehicleId:guid}/deactivate")]
    public static async Task<IResult> DeactivateVehicle(
        Guid vehicleId,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new DeactivateVehicle(context.ProviderId, vehicleId),
            cancellationToken
        );
        return NoContentResult(result);
    }

    [WolverinePost("/api/journeys/{journeyKey}/assignments")]
    public static async Task<IResult> AssignJourney(
        string journeyKey,
        AssignTripRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<AssignmentMutationResponse>>(
            new AssignJourney(context.ProviderId, context.AppUserId, journeyKey, request),
            cancellationToken
        );
        return AssignmentResult(result);
    }

    [WolverinePost("/api/trips/{tripNumber}/assignments")]
    public static async Task<IResult> AssignTrip(
        string tripNumber,
        AssignTripRequest request,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<AssignmentMutationResponse>>(
            new AssignSingleTrip(context.ProviderId, context.AppUserId, tripNumber, request),
            cancellationToken
        );
        return AssignmentResult(result);
    }

    [WolverineGet("/api/trips/{tripNumber}/assignments")]
    public static async Task<IResult> AssignmentHistory(
        string tripNumber,
        ClaimsPrincipal user,
        ApplicationDbContext db,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, db, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<
            DispatchManagementResult<IReadOnlyList<AssignmentResponse>>
        >(new GetAssignmentHistory(context.ProviderId, tripNumber), cancellationToken);
        return result.Outcome == DispatchManagementOutcome.Success
            ? Results.Ok(result.Value)
            : Results.NotFound();
    }

    private static IResult CreatedResult<T>(DispatchManagementResult<T> result) =>
        result.Outcome switch
        {
            DispatchManagementOutcome.Success => Results.Created(result.Location!, result.Value),
            DispatchManagementOutcome.NotFound => Results.NotFound(),
            _ => Results.BadRequest(new { message = result.Message }),
        };

    private static IResult NoContentResult(DispatchManagementResult<bool> result) =>
        result.Outcome switch
        {
            DispatchManagementOutcome.Success => Results.NoContent(),
            DispatchManagementOutcome.NotFound => Results.NotFound(),
            _ => Results.BadRequest(new { message = result.Message }),
        };

    private static IResult AssignmentResult(
        DispatchManagementResult<AssignmentMutationResponse> result
    ) =>
        result.Outcome switch
        {
            DispatchManagementOutcome.Success => Results.Ok(result.Value),
            DispatchManagementOutcome.NotFound => Results.NotFound(),
            DispatchManagementOutcome.Conflict => Results.Conflict(
                new { message = result.Message }
            ),
            _ => Results.BadRequest(new { message = result.Message }),
        };

    private static async Task<ProviderContext?> DispatcherContext(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var context = await ProviderContextResolver.ResolveActive(user, db, cancellationToken);
        return ProviderContextResolver.HasRole(context, "Dispatcher") ? context : null;
    }
}
