using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Update;

public sealed record UpdateUserCommand(Guid Id, string DisplayName, string[] Roles, bool IsActive, int Version)
    : ICommand<bool>;
