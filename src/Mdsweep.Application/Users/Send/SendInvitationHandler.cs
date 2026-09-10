using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Send;

public sealed class SendInvitationHandler(
    IRepository repository,
    IUserContext context,
    IIdentityAdministration identity,
    IClock clock
)
{
    public async Task<Result<InvitationModel>> Handle(SendInvitationCommand command, CancellationToken ct)
    {
        if (context.TenantId is null)
            return Result.Forbidden();
        var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(command.Id, ct);
        if (invitation is null || invitation.TenantId != context.TenantId)
            return Result.NotFound();
        if (invitation.Status != "Pending")
            return Result.Invalid(
                new ValidationError("invitation", "Only pending or expired invitations can be resent.")
            );
        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(invitation.TenantId, ct);
        string? error = null;
        try
        {
            await identity.InviteAsync(
                tenant!.KeycloakOrganizationId,
                invitation.Email,
                invitation.FirstName,
                invitation.LastName,
                ct
            );
        }
        catch (IdentityAdministrationException exception)
        {
            error = exception.Message;
        }
        invitation.RecordDelivery(clock.GetCurrentInstant(), error);
        return InvitationModel.From(invitation, clock.GetCurrentInstant());
    }
}
