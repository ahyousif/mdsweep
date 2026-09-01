using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.TripImports.Apply;

namespace Mdsweep.Api.Features.TripImports.Apply;

public sealed class ApplyTripImportEndpoint
{
    [Tags(TripImportConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.Dispatcher)]
    [WolverinePost(TripImportConstants.ApplyRoute)]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new ApplyTripImportCommand(id), ct);

        return result.ToEndpointResult(TripImportResponse.FromModel);
    }
}
