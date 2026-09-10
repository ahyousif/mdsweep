using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class GetPendingInvitationsHandler(
    IRepository repository,
    IUserContext actor,
    IIdentityAdministration identity,
    IClock clock
)
{
    public async Task<Result<PendingInvitationModel[]>> Handle(GetPendingInvitationsQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor.Subject))
            return Result.Unauthorized();
        VerifiedIdentity? verified;
        try
        {
            verified = await identity.GetVerifiedIdentityAsync(actor.Subject, ct);
        }
        catch (IdentityAdministrationException exception)
        {
            return Result.Invalid(new ValidationError("identity", exception.Message));
        }
        if (verified is null || verified.Subject != actor.Subject)
            return Result.Invalid(
                new ValidationError("email", "Verify your email through the invitation before accepting.")
            );
        var invitations = await repository.ListAsync(
            new InvitationsSpecification(email: verified.Email.ToLowerInvariant()),
            ct
        );
        var existing = await repository.SingleOrDefaultAsync(new UsersSpecification(subject: actor.Subject), ct);
        var memberships = existing is null
            ? []
            : await repository.ListAsync(new MembershipsSpecification(userId: existing.Id), ct);
        var pending = new List<PendingInvitationModel>();
        foreach (
            var invitation in invitations.Where(x =>
                x.ExpiresAt > clock.GetCurrentInstant() && !memberships.Any(m => m.TenantId == x.TenantId)
            )
        )
        {
            var tenant = await repository.GetByIdAsync<TenantAggregate, string>(invitation.TenantId, ct);
            pending.Add(
                new PendingInvitationModel(invitation.Id, tenant!.Name, invitation.Roles, invitation.ExpiresAt)
            );
        }
        return pending.ToArray();
    }
}

public sealed class AcceptInvitationHandler(
    IRepository repository,
    IUserContext actor,
    IIdentityAdministration identity,
    IClock clock
)
{
    public async Task<Result<bool>> Handle(AcceptInvitationCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor.Subject))
            return Result.Unauthorized();
        var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(command.Id, ct);
        if (invitation is null)
            return Result.NotFound();
        var existing = await repository.SingleOrDefaultAsync(new UsersSpecification(subject: actor.Subject), ct);
        if (invitation.Status == "Accepted" && existing is not null && invitation.AcceptedUserId == existing.Id)
            return true; // Retrying acceptance never restores deactivated access.
        if (invitation.Status != "Pending" || invitation.ExpiresAt <= clock.GetCurrentInstant())
            return Result.Invalid(
                new ValidationError(
                    "invitation",
                    "This invitation has expired or was revoked. Ask for a new invitation."
                )
            );
        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(invitation.TenantId, ct);
        try
        {
            var verified = await identity.GetVerifiedIdentityAsync(actor.Subject, ct);
            if (
                verified is null
                || verified.Subject != actor.Subject
                || !string.Equals(verified.Email, invitation.Email, StringComparison.OrdinalIgnoreCase)
                || !await identity.IsOrganizationMemberAsync(actor.Subject, tenant!.KeycloakOrganizationId, ct)
            )
                return Result.Invalid(
                    new ValidationError(
                        "invitation",
                        "Open the invitation email and finish signup with the invited email address first."
                    )
                );
        }
        catch (IdentityAdministrationException exception)
        {
            return Result.Invalid(new ValidationError("identity", exception.Message));
        }
        var emailOwner = await repository.SingleOrDefaultAsync(
            new UsersSpecification(email: invitation.Email.ToLowerInvariant()),
            ct
        );
        if (emailOwner is not null && emailOwner.Id != existing?.Id)
            return Result.Invalid(
                new ValidationError(
                    "email",
                    "This email already belongs to another identity. Ask an Administrator to review your access."
                )
            );
        if (
            existing is not null
            && await repository.SingleOrDefaultAsync(new MembershipsSpecification(invitation.TenantId, existing.Id), ct)
                is not null
        )
            return Result.Invalid(
                new ValidationError(
                    "user",
                    "You already have a membership in this Tenant. Ask an Administrator to review your access."
                )
            );
        var user =
            existing
            ?? UserAggregate.Create(
                invitation.FirstName,
                invitation.LastName,
                actor.Subject,
                invitation.Email.ToLowerInvariant()
            );
        var membership = TenantMembership.Create(invitation.TenantId, user.Id, invitation.Roles);
        membership.SetDisplayName($"{invitation.FirstName} {invitation.LastName}");
        invitation.Accept(user.Id, clock.GetCurrentInstant());
        if (existing is null)
            await repository.AddAsync(user, ct);
        await repository.AddAsync(membership, ct);
        return true;
    }
}
