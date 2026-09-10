using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Users.Pending;

public sealed record GetPendingInvitationsQuery : IQuery<PendingInvitationModel[]>;
