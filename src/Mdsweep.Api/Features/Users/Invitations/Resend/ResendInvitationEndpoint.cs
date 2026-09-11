using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users.Invitations.Resend;

namespace Mdsweep.Api.Features.Users.Invitations.Resend;

public sealed class ResendInvitationEndpoint
{
    [Tags(UserConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePost(UserConstants.Invitations.ResendRoute)]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new ResendInvitationCommand(id), ct);

        return result.ToEndpointResult(_ => Results.NoContent());
    }
}
