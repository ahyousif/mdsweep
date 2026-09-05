using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.TripImports.Import;
using Mdsweep.Application.Trips.Scheduling;

namespace Mdsweep.Api.Features.Trips.Import;

public sealed class ImportTripsEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsImport)]
    [WolverinePost(TripConstants.ImportRoute)]
    public static async Task<IResult> Post(
        IFormFile file,
        IMessageBus bus,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        await using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);

        var result = await bus.SendAsync(
            new ImportTripsCommand(file.FileName, file.ContentType, content.ToArray()),
            ct
        );

        if (!result.IsSuccess)
        {
            return result.ToEndpointResult(value => Results.Ok(ImportTripsResponse.FromResult(value)));
        }

        foreach (var tripId in result.Value.SchedulingTripIds)
        {
            await bus.PublishAsync(new CalculateScheduledPickupTimeCommand(tripId));
        }

        return Results.Ok(ImportTripsResponse.FromResult(result.Value));
    }
}
