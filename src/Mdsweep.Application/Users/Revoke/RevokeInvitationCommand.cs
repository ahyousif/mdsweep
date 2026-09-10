using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Revoke;

public sealed record RevokeInvitationCommand(Guid Id) : ICommand<bool>;
