using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;
using Mdsweep.Application.Users.Pending;

namespace Mdsweep.Api.Features.Users.Pending;

public static class GetPendingInvitationsEndpoint
{
    public static IEndpointRouteBuilder MapPendingInvitations(this IEndpointRouteBuilder endpoints)
    {
        // Invitations are available before Tenant selection.
        endpoints.MapGet("/api/invitation", Get).RequireAuthorization().WithTags("Users");
        return endpoints;
    }

    public static async Task<IResult> Get(IMessageBus bus, CancellationToken ct) =>
        (await bus.SendAsync(new GetPendingInvitationsQuery(), ct)).ToEndpointResult(x => Results.Ok(x));
}
