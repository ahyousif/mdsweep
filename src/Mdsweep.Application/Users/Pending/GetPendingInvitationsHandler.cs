using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Tenants;

namespace Mdsweep.Application.Users.Pending;

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
