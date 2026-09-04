using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.TripImports.Import;

namespace Mdsweep.Api.Features.Trips.Import;

public sealed class ImportTripsEndpoint
{
    [Tags(TripConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsImport)]
    [WolverinePost(TripConstants.ImportRoute)]
    public static async Task<IResult> Post(IFormFile file, IMessageBus bus, CancellationToken ct)
    {
        await using var content = new MemoryStream();
        await file.CopyToAsync(content, ct);

        var result = await bus.SendAsync(
            new ImportTripsCommand(file.FileName, file.ContentType, content.ToArray()),
            ct
        );

        return result.ToEndpointResult(value => Results.Ok(ImportTripsResponse.FromResult(value)));
    }
}
