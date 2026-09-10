using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;
using Mdsweep.Application.Users.Accept;

namespace Mdsweep.Api.Features.Users.Accept;

public static class AcceptInvitationEndpoint
{
    public static IEndpointRouteBuilder MapInvitationAcceptance(this IEndpointRouteBuilder endpoints)
    {
        // Invitations are available before Tenant selection.
        endpoints.MapPost("/api/invitation/{id:guid}/accept", Post).RequireAuthorization().WithTags("Users");
        return endpoints;
    }

    public static async Task<IResult> Post(Guid id, IMessageBus bus, CancellationToken ct) =>
        (await bus.SendAsync(new AcceptInvitationCommand(id), ct)).ToEndpointResult(_ => Results.NoContent());
}
