using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users.Invitations.Cancel;

namespace Mdsweep.Api.Features.Users.Invitations.Cancel;

public sealed class CancelInvitationEndpoint
{
    [Tags(UserConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverineDelete(UserConstants.Invitations.IdRoute)]
    public static async Task<IResult> Delete(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new CancelInvitationCommand(id), ct);

        return result.ToEndpointResult(_ => Results.NoContent());
    }
}
