using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class InviteUserHandler(IRepository repository, IUserContext context, IClock clock)
{
    public async Task<Result<Guid>> Handle(InviteUserCommand command, CancellationToken ct)
    {
        if (context.TenantId is null)
            return Result.Forbidden();
        if (!TenantMembership.AreValidRoles(command.Roles))
            return Result.Invalid(new ValidationError("roles", "Select one or two distinct roles."));
        var email = command.Email.ToLowerInvariant();
        var existing = await repository.SingleOrDefaultAsync(new UsersSpecification(email: email), ct);
        if (
            existing is not null
            && await repository.SingleOrDefaultAsync(new MembershipsSpecification(context.TenantId, existing.Id), ct)
                is not null
        )
            return Result.Invalid(
                new ValidationError(
                    "email",
                    "This email already belongs to a User. Manage their existing access instead."
                )
            );
        if (
            await repository.SingleOrDefaultAsync(new InvitationsSpecification(context.TenantId, email), ct) is not null
        )
            return Result.Invalid(
                new ValidationError("email", "An invitation already exists for this email. Resend or revoke it first.")
            );
        var invitation = InvitationAggregate.Create(
            context.TenantId,
            email,
            command.FirstName,
            command.LastName,
            command.Roles,
            clock.GetCurrentInstant()
        );
        await repository.AddAsync(invitation, ct);
        return invitation.Id;
    }
}

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
