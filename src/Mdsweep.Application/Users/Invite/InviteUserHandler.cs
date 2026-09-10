using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invite;

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
