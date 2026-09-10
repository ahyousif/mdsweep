using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Accept;

public sealed record AcceptInvitationCommand(Guid Id) : ICommand<bool>;
