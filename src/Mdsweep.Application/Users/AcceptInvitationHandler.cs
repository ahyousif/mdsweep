using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class GetPendingInvitationHandler(IRepository repository, IAccessActor actor,
    IIdentityAdministration identity, IClock clock)
{
    public async Task<Result<PendingInvitationModel>> Handle(GetPendingInvitationQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor.Subject)) return Result.Unauthorized();
        var existing = await repository.SingleOrDefaultAsync(new UsersSpecification(subject: actor.Subject), ct);
        if (existing is not null) return Result.NotFound();
        VerifiedIdentity? verified;
        try { verified = await identity.GetVerifiedIdentityAsync(actor.Subject, ct); }
        catch (IdentityAdministrationException exception)
        {
            return Result.Invalid(new ValidationError("identity", exception.Message));
        }
        if (verified is null) return Result.Invalid(new ValidationError("email", "Verify your email through the invitation before accepting."));
        var invitation = await repository.SingleOrDefaultAsync(new InvitationsSpecification(email: verified.Email.ToLowerInvariant()), ct);
        if (invitation is null || invitation.ExpiresAt <= clock.GetCurrentInstant()) return Result.NotFound();
        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(invitation.TenantId, ct);
        return new PendingInvitationModel(invitation.Id, tenant!.Name, invitation.Roles, invitation.ExpiresAt);
    }
}

public sealed class AcceptInvitationHandler(IRepository repository, IAccessActor actor,
    IIdentityAdministration identity, IClock clock)
{
    public async Task<Result<bool>> Handle(AcceptInvitationCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor.Subject)) return Result.Unauthorized();
        var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(command.Id, ct);
        if (invitation is null) return Result.NotFound();
        var existing = await repository.SingleOrDefaultAsync(new UsersSpecification(subject: actor.Subject), ct);
        if (invitation.Status == "Accepted" && existing is not null && invitation.AcceptedUserId == existing.Id)
            return true; // A retried acceptance never duplicates a User or restores deactivated access.
        if (invitation.Status != "Pending" || invitation.ExpiresAt <= clock.GetCurrentInstant())
            return Result.Invalid(new ValidationError("invitation", "This invitation has expired or was revoked. Ask for a new invitation."));
        if (existing is not null)
            return Result.Invalid(new ValidationError("user", "Your account already belongs to a Tenant. Sign in with the invited account."));
        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(invitation.TenantId, ct);
        try
        {
            var verified = await identity.GetVerifiedIdentityAsync(actor.Subject, ct);
            if (verified is null || !string.Equals(verified.Email, invitation.Email, StringComparison.OrdinalIgnoreCase)
                || !await identity.IsOrganizationMemberAsync(actor.Subject, tenant!.KeycloakOrganizationId, ct))
                return Result.Invalid(new ValidationError("invitation", "Open the invitation email and finish signup with the invited email address first."));
        }
        catch (IdentityAdministrationException exception)
        {
            return Result.Invalid(new ValidationError("identity", exception.Message));
        }
        if (await repository.SingleOrDefaultAsync(new UsersSpecification(email: invitation.Email.ToLowerInvariant()), ct) is not null)
            return Result.Invalid(new ValidationError("email", "This email already belongs to a User. Ask an Administrator to review your access."));
        var user = UserAggregate.Create(invitation.FirstName, invitation.LastName, actor.Subject, invitation.TenantId, invitation.Email);
        user.Record(actor.Subject, "Invitation accepted", clock.GetCurrentInstant(), string.Join(", ", invitation.Roles));
        invitation.Accept(user.Id, actor.Subject, clock.GetCurrentInstant());
        await repository.AddAsync(user, ct);
        await repository.AddAsync(TenantMembership.Create(invitation.TenantId, user.Id, invitation.Roles), ct);
        return true;
    }
}
