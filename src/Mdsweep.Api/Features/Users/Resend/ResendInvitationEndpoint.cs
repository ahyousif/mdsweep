using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;

namespace Mdsweep.Api.Features.Users.Resend;

public sealed class ResendInvitationEndpoint
{
    [Tags("Users")]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePost("/users/invitations/{id:guid}/resend")]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        return (await bus.SendAsync(new SendInvitationCommand(id), ct)).ToEndpointResult(x => Results.Ok(x));
    }
}
