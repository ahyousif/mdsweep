using Mdsweep.Application.Users.Update;
using Microsoft.AspNetCore.Mvc;

namespace Mdsweep.Api.Features.Users.Update;

public sealed record UpdateUserRequest
{
    [FromRoute]
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = null!;
    public bool IsActive { get; init; }
    public string[] Roles { get; init; } = [];

    internal UpdateUserCommand ToCommand()
    {
        return new UpdateUserCommand(UserId, DisplayName, Roles, IsActive);
    }
}
