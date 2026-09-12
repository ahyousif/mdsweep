using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Invitations.Resend;

public sealed record ResendInvitationCommand(Guid Id) : ICommand;
