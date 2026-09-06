using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class InviteUserHandler(IRepository repository, UserManagementAccess access, IClock clock)
{
    public async Task<Result<Guid>> Handle(InviteUserCommand command, CancellationToken ct)
    {
        var manager = await access.Manager(ct);
        if (manager is null || !UserManagementAccess.CanManage(manager.Value.Administrator, command.Roles))
            return Result.Forbidden();
        if (!UserManagementAccess.ValidRoles(command.Roles)) return Result.Invalid(new ValidationError("roles", "Select one or two distinct roles."));
        var email = command.Email.ToLowerInvariant();
        if (await repository.SingleOrDefaultAsync(new UsersSpecification(email: email), ct) is not null)
            return Result.Invalid(new ValidationError("email", "This email already belongs to a User. Manage their existing access instead."));
        if (await repository.SingleOrDefaultAsync(new InvitationsSpecification(email: email), ct) is not null)
            return Result.Invalid(new ValidationError("email", "An invitation already exists for this email. Resend or revoke it first."));
        var actor = manager.Value.User;
        var invitation = InvitationAggregate.Create(actor.TenantId, command.Email, command.FirstName,
            command.LastName, command.Roles, actor.KeycloakUserId, clock.GetCurrentInstant());
        await repository.AddAsync(invitation, ct);
        return invitation.Id;
    }
}

public sealed class SendInvitationHandler(IRepository repository, UserManagementAccess access,
    IIdentityAdministration identity, IClock clock)
{
    public async Task<Result<InvitationModel>> Handle(SendInvitationCommand command, CancellationToken ct)
    {
        var manager = await access.Manager(ct);
        if (manager is null) return Result.Forbidden();
        var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(command.Id, ct);
        if (invitation is null || invitation.TenantId != manager.Value.User.TenantId) return Result.NotFound();
        if (!UserManagementAccess.CanManage(manager.Value.Administrator, invitation.Roles)) return Result.Forbidden();
        if (invitation.Status != "Pending")
            return Result.Invalid(new ValidationError("invitation", "Only pending or expired invitations can be resent."));
        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(invitation.TenantId, ct);
        string? error = null;
        try
        {
            await identity.InviteAsync(tenant!.KeycloakOrganizationId, invitation.Email, invitation.FirstName, invitation.LastName, ct);
        }
        catch (IdentityAdministrationException exception) { error = exception.Message; }
        invitation.RecordDelivery(manager.Value.User.KeycloakUserId, clock.GetCurrentInstant(), error);
        return UserManagementAccess.Model(invitation, clock.GetCurrentInstant());
    }
}

public sealed class RevokeInvitationHandler(IRepository repository, UserManagementAccess access, IClock clock)
{
    public async Task<Result<bool>> Handle(RevokeInvitationCommand command, CancellationToken ct)
    {
        var manager = await access.Manager(ct);
        if (manager is null) return Result.Forbidden();
        var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(command.Id, ct);
        if (invitation is null || invitation.TenantId != manager.Value.User.TenantId) return Result.NotFound();
        if (!UserManagementAccess.CanManage(manager.Value.Administrator, invitation.Roles)) return Result.Forbidden();
        if (invitation.Status == "Accepted")
            return Result.Invalid(new ValidationError("invitation", "This invitation was accepted. Deactivate the User to remove access."));
        invitation.Revoke(manager.Value.User.KeycloakUserId, clock.GetCurrentInstant());
        return true;
    }
}
