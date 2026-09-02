using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.TripImports.Get;

namespace Mdsweep.Api.Features.TripImports.Get;

public sealed class GetTripImportEndpoint
{
    [Tags(TripImportConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsImport)]
    [WolverineGet(TripImportConstants.IdRoute)]
    public static async Task<IResult> Get(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new GetTripImportQuery(id), ct);

        return result.ToEndpointResult(TripImportResponse.FromModel);
    }
}
