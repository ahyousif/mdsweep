using Mdsweep.Api.Common.Extensions;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.TripImports.Get;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.TripImports.Get;

public sealed class GetTripImportEndpoint
{
    [Tags(TripImportConstants.Tag)]
    [Authorize(Policy = TenantAuthorizationPolicies.Dispatcher)]
    [WolverineGet(TripImportConstants.IdRoute)]
    public static async Task<IResult> Get(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new GetTripImportQuery(id), ct);
        return result.ToEndpointResult(TripImportResponse.FromModel);
    }
}
