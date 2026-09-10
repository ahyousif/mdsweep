using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;
using Mdsweep.Application.Users.ResetPassword;

namespace Mdsweep.Api.Features.Users.ResetPassword;

public sealed class ResetUserPasswordEndpoint
{
    [Tags("Users")]
    [Authorize(Policy = AuthorizationPolicies.UsersManage)]
    [WolverinePost("/users/{id:guid}/password-reset")]
    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct)
    {
        return (await bus.SendAsync(new ResetUserPasswordCommand(id), ct)).ToEndpointResult(_ => Results.NoContent());
    }
}
