using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Invitations.Accept;

public sealed record AcceptInvitationCommand(string Token) : ICommand;
