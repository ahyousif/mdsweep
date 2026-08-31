using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.Dispatch;

namespace Mdsweep.Api.Features.Dispatch;

public static class DispatchManagementEndpoints
{
    [WolverineGet("/api/drivers")]
    public static async Task<IResult> ListDrivers(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        return Results.Ok(
            await bus.InvokeAsync<List<DriverResponse>>(
                new ListDrivers(context.TenantId),
                cancellationToken
            )
        );
    }

    [WolverinePost("/api/drivers")]
    public static async Task<IResult> CreateDriver(
        CreateDriverRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<DriverResponse>>(
            new CreateDriver(context.TenantId, request),
            cancellationToken
        );
        return CreatedResult(result);
    }

    [WolverinePost("/api/drivers/access")]
    public static async Task<IResult> CreateDriverAccess(
        CreateDriverAccessRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<DriverResponse>>(
            new CreateDriverAccess(context.TenantId, request),
            cancellationToken
        );
        return CreatedResult(result);
    }

    [WolverinePost("/api/drivers/{driverId:guid}/reset-access")]
    public static async Task<IResult> ResetDriverAccess(
        Guid driverId,
        ResetDriverAccessRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new ResetDriverAccess(context.TenantId, driverId, request),
            cancellationToken
        );
        return NoContentResult(result);
    }

    [WolverinePost("/api/drivers/{driverId:guid}/deactivate")]
    public static async Task<IResult> DeactivateDriver(
        Guid driverId,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new DeactivateDriver(context.TenantId, driverId),
            cancellationToken
        );
        return NoContentResult(result);
    }

    [WolverineGet("/api/vehicles")]
    public static async Task<IResult> ListVehicles(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        return Results.Ok(
            await bus.InvokeAsync<List<VehicleResponse>>(
                new ListVehicles(context.TenantId),
                cancellationToken
            )
        );
    }

    [WolverinePost("/api/vehicles")]
    public static async Task<IResult> CreateVehicle(
        CreateVehicleRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<VehicleResponse>>(
            new CreateVehicle(context.TenantId, request),
            cancellationToken
        );
        return CreatedResult(result);
    }

    [WolverinePost("/api/vehicles/{vehicleId:guid}/deactivate")]
    public static async Task<IResult> DeactivateVehicle(
        Guid vehicleId,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<bool>>(
            new DeactivateVehicle(context.TenantId, vehicleId),
            cancellationToken
        );
        return NoContentResult(result);
    }

    [WolverinePost("/api/journeys/{journeyKey}/assignments")]
    public static async Task<IResult> AssignJourney(
        string journeyKey,
        AssignTripRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<AssignmentMutationResponse>>(
            new AssignJourney(context.TenantId, context.UserId, journeyKey, request),
            cancellationToken
        );
        return AssignmentResult(result);
    }

    [WolverinePost("/api/trips/{tripNumber}/assignments")]
    public static async Task<IResult> AssignTrip(
        string tripNumber,
        AssignTripRequest request,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<DispatchManagementResult<AssignmentMutationResponse>>(
            new AssignSingleTrip(context.TenantId, context.UserId, tripNumber, request),
            cancellationToken
        );
        return AssignmentResult(result);
    }

    [WolverineGet("/api/trips/{tripNumber}/assignments")]
    public static async Task<IResult> AssignmentHistory(
        string tripNumber,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await DispatcherContext(user, tenantAccess, cancellationToken);
        if (context is null)
            return Results.Forbid();
        var result = await bus.InvokeAsync<
            DispatchManagementResult<IReadOnlyList<AssignmentResponse>>
        >(new GetAssignmentHistory(context.TenantId, tripNumber), cancellationToken);
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

    private static async Task<TenantContext?> DispatcherContext(
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        CancellationToken cancellationToken
    )
    {
        var context = await TenantContextResolver.ResolveActive(user, tenantAccess, cancellationToken);
        return TenantContextResolver.HasRole(context, "Dispatcher") ? context : null;
    }
}
