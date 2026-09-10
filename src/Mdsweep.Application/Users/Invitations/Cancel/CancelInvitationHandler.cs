using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invitations.Cancel;

public sealed class CancelInvitationHandler(IRepository repository)
{
    public async Task<Result> Handle(CancelInvitationCommand command, CancellationToken ct)
    {
        var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(command.Id, ct);

        if (invitation is null)
        {
            return Result.NotFound();
        }

        invitation.Cancel();

        return Result.Success();
    }
}
