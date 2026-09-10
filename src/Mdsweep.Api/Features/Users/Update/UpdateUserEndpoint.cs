using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;

namespace Mdsweep.Api.Features.Users.Update;

public sealed class UpdateUserEndpoint
{
    [Tags("Users")]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePut("/users/{id:guid}")]
    public static async Task<IResult> Put(Guid id, UpdateUserRequest request, IMessageBus bus, CancellationToken ct)
    {
        return (
            await bus.SendAsync(
                new UpdateUserCommand(id, request.DisplayName, request.Roles, request.IsActive, request.Version),
                ct
            )
        ).ToEndpointResult(_ => Results.NoContent());
    }
}
