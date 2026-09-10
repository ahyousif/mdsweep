using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;
using Mdsweep.Application.Users.Invite;
using Mdsweep.Application.Users.Send;

namespace Mdsweep.Api.Features.Users.Invite;

public sealed class InviteUserEndpoint
{
    [Tags("Users")]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePost("/users/invitations")]
    public static async Task<IResult> Post(InviteUserRequest request, IMessageBus bus, CancellationToken ct)
    {
        // Commit the invitation before attempting delivery. A failed email remains visible and retryable.
        var created = await bus.SendAsync(
            new InviteUserCommand(request.Email, request.FirstName, request.LastName, request.Roles),
            ct
        );
        return await created.ToEndpointResultAsync(async id =>
            (await bus.SendAsync(new SendInvitationCommand(id), ct)).ToEndpointResult(x =>
                Results.Created($"/api/users/invitations/{id}", x)
            )
        );
    }
}
