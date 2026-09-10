using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Send;

public sealed record SendInvitationCommand(Guid Id) : ICommand<InvitationModel>;
