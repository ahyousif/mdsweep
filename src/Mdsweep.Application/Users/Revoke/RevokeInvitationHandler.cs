using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Revoke;

public sealed class RevokeInvitationHandler(IRepository repository, IUserContext context)
{
    public async Task<Result<bool>> Handle(RevokeInvitationCommand command, CancellationToken ct)
    {
        if (context.TenantId is null)
            return Result.Forbidden();
        var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(command.Id, ct);
        if (invitation is null || invitation.TenantId != context.TenantId)
            return Result.NotFound();
        if (invitation.Status == "Accepted")
            return Result.Invalid(
                new ValidationError("invitation", "This invitation was accepted. Deactivate the User to remove access.")
            );
        invitation.Revoke();
        return true;
    }
}
