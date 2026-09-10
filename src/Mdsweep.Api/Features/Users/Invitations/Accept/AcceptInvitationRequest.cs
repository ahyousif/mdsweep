using Mdsweep.Application.Users.Invitations.Accept;

namespace Mdsweep.Api.Features.Users.Invitations.Accept;

public sealed record AcceptInvitationRequest
{
    public required string Token { get; init; }

    internal AcceptInvitationCommand ToCommand()
    {
        return new AcceptInvitationCommand(Token);
    }
}
