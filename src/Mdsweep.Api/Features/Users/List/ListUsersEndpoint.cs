using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;
using Mdsweep.Application.Users.List;

namespace Mdsweep.Api.Features.Users.List;

public sealed class ListUsersEndpoint
{
    [Tags("Users")]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverineGet("/users")]
    public static async Task<IResult> Get(IMessageBus bus, CancellationToken ct) =>
        (await bus.SendAsync(new ListUsersQuery(), ct)).ToEndpointResult(x => Results.Ok(x));
}
