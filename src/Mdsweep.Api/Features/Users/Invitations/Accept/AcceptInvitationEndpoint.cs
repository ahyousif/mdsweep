using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Users.Invitations.Accept;

public static class AcceptInvitationEndpoint
{
    [Tags(UserConstants.Tag)]
    [Authorize]
    [NotTenanted]
    [WolverinePost(UserConstants.Invitations.Accept)]
    public static async Task<IResult> Post(AcceptInvitationRequest req, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(req.ToCommand(), ct);

        return result.ToEndpointResult();
    }
}
