using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Invitations.Invite;

public sealed record InviteUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string[] Roles,
    string? DisplayName = null
) : ICommand;
