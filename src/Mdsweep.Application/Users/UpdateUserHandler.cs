using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class UpdateUserHandler(IRepository repository, IUserContext context, IClock clock)
{
    public async Task<Result<bool>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        if (context.TenantId is null)
            return Result.Forbidden();
        var user = await repository.GetByIdAsync<UserAggregate, Guid>(command.Id, ct);
        if (user is null)
            return Result.NotFound();
        var membership = await repository.SingleOrDefaultAsync(
            new MembershipsSpecification(context.TenantId, user.Id),
            ct
        );
        if (membership is null)
            return Result.NotFound();
        if (!TenantMembership.AreValidRoles(command.Roles))
            return Result.Invalid(new ValidationError("roles", "Select one or two distinct roles."));
        if (membership.Version != command.Version)
            return Result.Conflict();
        if (
            user.KeycloakUserId == context.Subject
            && (!command.IsActive || !command.Roles.Contains(TenantRoles.Administrator))
        )
            return Result.Invalid(
                new ValidationError("roles", "You cannot deactivate yourself or remove your own Administrator role.")
            );
        var now = clock.GetCurrentInstant();
        var actor = context.Subject;
        if ((membership.DisplayName ?? $"{user.FirstName} {user.LastName}") != command.DisplayName)
        {
            membership.SetDisplayName(command.DisplayName);
            membership.Record(actor, "Display name updated", now);
        }
        if (!membership.Roles.Order().SequenceEqual(command.Roles.Order()))
        {
            var previousRoles = string.Join(", ", membership.Roles);
            membership.SetRoles(command.Roles);
            membership.Record(actor, "Roles changed", now, $"{previousRoles} → {string.Join(", ", command.Roles)}");
        }
        if (membership.IsActive != command.IsActive)
        {
            membership.SetActive(command.IsActive);
            membership.Record(actor, command.IsActive ? "Reactivated" : "Deactivated", now);
        }
        return true;
    }
}

public sealed class ResetUserPasswordHandler(
    IRepository repository,
    IUserContext context,
    IIdentityAdministration identity,
    IClock clock
)
{
    public async Task<Result<bool>> Handle(ResetUserPasswordCommand command, CancellationToken ct)
    {
        if (context.TenantId is null)
            return Result.Forbidden();
        var user = await repository.GetByIdAsync<UserAggregate, Guid>(command.Id, ct);
        if (user is null)
            return Result.NotFound();
        var membership = await repository.SingleOrDefaultAsync(
            new MembershipsSpecification(context.TenantId, user.Id),
            ct
        );
        if (membership is null)
            return Result.NotFound();
        if (!membership.IsActive)
            return Result.Invalid(
                new ValidationError("user", "Reactivate this User before requesting a password reset.")
            );
        try
        {
            await identity.SendPasswordResetAsync(user.KeycloakUserId, ct);
        }
        catch (IdentityAdministrationException exception)
        {
            return Result.Invalid(new ValidationError("email", exception.Message));
        }
        membership.Record(context.Subject, "Password reset email sent", clock.GetCurrentInstant());
        return true;
    }
}
