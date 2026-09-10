using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Invite;

public sealed record InviteUserCommand(string Email, string FirstName, string LastName, string[] Roles)
    : ICommand<Guid>;
