using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Users.Update;

public sealed class UpdateUserEndpoint
{
    [Tags(UserConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePut(UserConstants.IdRoute)]
    public static async Task<IResult> Put(Guid id, UpdateUserRequest req, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(req.ToCommand(id), ct);

        return result.ToEndpointResult();
    }
}
