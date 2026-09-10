using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Update;

public sealed record UpdateUserCommand(Guid UserId, string DisplayName, string[] Roles, bool IsActive) : ICommand;
