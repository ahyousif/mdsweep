using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.TripImports.Import;

namespace Mdsweep.Api.Features.Trips.Import;

public sealed class ImportTripsEndpoint
{
    [Tags("Trips")]
    [Authorize(Policy = AuthorizationPolicies.TripsImport)]
    [WolverinePost("/trips/import")]
    public static async Task<IResult> Post([AsParameters] ImportTripsRequest request, IMessageBus bus, CancellationToken ct)
    {
        var file = request.File!;
        await using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);
        var result = await bus.SendAsync(new ImportTripsCommand(file.FileName, file.ContentType, content.ToArray()), ct);
        return result.ToEndpointResult(value => Results.Ok(ImportTripsResponse.FromResult(value)));
    }
}
