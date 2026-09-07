using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Users;

namespace Mdsweep.Api.Features.Users;

[Tags("Users")]
[Authorize(Policy = AuthorizationPolicies.UsersManage)]
public static class UserEndpoints
{
    [WolverineGet("/users")]
    public static async Task<IResult> List(IMessageBus bus, CancellationToken ct) =>
        (await bus.SendAsync(new ListUsersQuery(), ct)).ToEndpointResult(x => Results.Ok(x));

    [WolverinePost("/users/invitations")]
    public static async Task<IResult> Invite(InviteUserRequest request, IMessageBus bus, CancellationToken ct)
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

    [WolverinePost("/users/invitations/{id:guid}/resend")]
    public static async Task<IResult> Resend(Guid id, IMessageBus bus, CancellationToken ct)
    {
        return (await bus.SendAsync(new SendInvitationCommand(id), ct)).ToEndpointResult(x => Results.Ok(x));
    }

    [WolverinePost("/users/invitations/{id:guid}/revoke")]
    public static async Task<IResult> Revoke(Guid id, IMessageBus bus, CancellationToken ct)
    {
        return (await bus.SendAsync(new RevokeInvitationCommand(id), ct)).ToEndpointResult(_ => Results.NoContent());
    }

    [WolverinePut("/users/{id:guid}")]
    public static async Task<IResult> Update(Guid id, UpdateUserRequest request, IMessageBus bus, CancellationToken ct)
    {
        return (
            await bus.SendAsync(
                new UpdateUserCommand(
                    id,
                    request.FirstName,
                    request.LastName,
                    request.Roles,
                    request.IsActive,
                    request.Version
                ),
                ct
            )
        ).ToEndpointResult(_ => Results.NoContent());
    }

    [WolverinePost("/users/{id:guid}/password-reset")]
    public static async Task<IResult> ResetPassword(Guid id, IMessageBus bus, CancellationToken ct)
    {
        return (await bus.SendAsync(new ResetUserPasswordCommand(id), ct)).ToEndpointResult(_ => Results.NoContent());
    }

    [WolverineGet("/users/{id:guid}/history")]
    public static async Task<IResult> UserHistory(Guid id, IMessageBus bus, CancellationToken ct) =>
        (await bus.SendAsync(new GetAccessHistoryQuery(id, false), ct)).ToEndpointResult(x => Results.Ok(x));

    [WolverineGet("/users/invitations/{id:guid}/history")]
    public static async Task<IResult> InvitationHistory(Guid id, IMessageBus bus, CancellationToken ct) =>
        (await bus.SendAsync(new GetAccessHistoryQuery(id, true), ct)).ToEndpointResult(x => Results.Ok(x));
}

public static class InvitationAcceptanceEndpoints
{
    public static IEndpointRouteBuilder MapInvitationAcceptance(this IEndpointRouteBuilder endpoints)
    {
        // Invitations may be accepted before Tenant selection or while accessing another Tenant.
        var group = endpoints.MapGroup("/api/invitation").RequireAuthorization().WithTags("Users");
        group.MapGet(
            "",
            async (IMessageBus bus, CancellationToken ct) =>
                (await bus.SendAsync(new GetPendingInvitationsQuery(), ct)).ToEndpointResult(x => Results.Ok(x))
        );
        group.MapPost(
            "/{id:guid}/accept",
            async (Guid id, IMessageBus bus, CancellationToken ct) =>
            {
                return (await bus.SendAsync(new AcceptInvitationCommand(id), ct)).ToEndpointResult(_ =>
                    Results.NoContent()
                );
            }
        );
        return endpoints;
    }
}
