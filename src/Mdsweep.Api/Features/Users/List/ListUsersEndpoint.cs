using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users.List;

namespace Mdsweep.Api.Features.Users.List;

public sealed class ListUsersEndpoint
{
    [Tags(UserConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverineGet(UserConstants.Index)]
    public static async Task<IResult> Get(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new ListUsersQuery(), ct);

        return result.ToEndpointResult(x => Results.Ok(x));
    }
}
