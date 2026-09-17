using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Mdsweep.Api.Features.Passengers.List;

public static class ListPassengersEndpoint
{
    [Tags(PassengerConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.PassengersManage)]
    [WolverineGet(PassengerConstants.Route)]
    public static async Task<IResult> Get([FromQuery] ListPassengersRequest request, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(request.ToQuery(), ct);

        return result.ToEndpointResult(value => new
        {
            value.Items,
            value.TotalCount,
            value.Page,
            value.PageSize,
            value.TotalPages,
        });
    }
}
