using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Invitations.Cancel;

public sealed record CancelInvitationCommand(Guid Id) : ICommand;
