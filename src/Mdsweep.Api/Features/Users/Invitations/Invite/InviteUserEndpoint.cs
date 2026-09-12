using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Users.Invitations.Invite;

public sealed class InviteUserEndpoint
{
    [Tags(UserConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePost(UserConstants.Invitations.Route)]
    public static async Task<IResult> Post(InviteUserRequest req, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(req.ToCommand(), ct);

        return result.ToEndpointResult();
    }
}
