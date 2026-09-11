using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invitations.Cancel;

public sealed class CancelInvitationHandler(IRepository repository)
{
    public async Task<Result> Handle(CancelInvitationCommand command, TenantId tenantId, CancellationToken ct)
    {
        var invitation = await repository.SingleOrDefaultAsync(
            new InvitationsSpecification()
                .WithId(command.Id)
                .WithTenantId(tenantId.Value)
                .WithStatus(InvitationStatus.Pending)
                .Build(),
            ct
        );

        if (invitation is null)
        {
            return Result.NotFound();
        }

        invitation.Cancel();

        return Result.Success();
    }
}
