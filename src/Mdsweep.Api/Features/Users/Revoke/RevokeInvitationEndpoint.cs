using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;
using Mdsweep.Application.Users.Revoke;

namespace Mdsweep.Api.Features.Users.Revoke;

public sealed class RevokeInvitationEndpoint
{
    [Tags("Users")]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePost("/users/invitations/{id:guid}/revoke")]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        return (await bus.SendAsync(new RevokeInvitationCommand(id), ct)).ToEndpointResult(_ => Results.NoContent());
    }
}
