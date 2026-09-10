using Mdsweep.Application.Users.Invitations.Invite;

namespace Mdsweep.Api.Features.Users.Invitations.Invite;

public sealed record InviteUserRequest
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string[] Roles { get; init; }

    internal InviteUserCommand ToCommand()
    {
        return new InviteUserCommand(Email, FirstName, LastName, Roles);
    }
}
