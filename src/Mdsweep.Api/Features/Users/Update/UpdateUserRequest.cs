using Mdsweep.Application.Users.Update;

namespace Mdsweep.Api.Features.Users.Update;

public sealed record UpdateUserRequest
{
    public string DisplayName { get; init; } = null!;
    public bool IsActive { get; init; }
    public string[] Roles { get; init; } = [];

    internal UpdateUserCommand ToCommand(Guid userId)
    {
        return new UpdateUserCommand(userId, DisplayName, Roles, IsActive);
    }
}
