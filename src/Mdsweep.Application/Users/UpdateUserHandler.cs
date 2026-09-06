using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class UpdateUserHandler(IRepository repository, UserManagementAccess access, IClock clock)
{
    public async Task<Result<bool>> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var manager = await access.Manager(ct);
        if (manager is null) return Result.Forbidden();
        var user = await repository.GetByIdAsync<UserAggregate, Guid>(command.Id, ct);
        if (user is null || user.TenantId != manager.Value.User.TenantId) return Result.NotFound();
        var membership = await repository.SingleOrDefaultAsync(new MembershipsSpecification(user.TenantId, user.Id), ct);
        if (membership is null) return Result.NotFound();
        if (!UserManagementAccess.CanManage(manager.Value.Administrator, membership.Roles)
            || !UserManagementAccess.CanManage(manager.Value.Administrator, command.Roles)) return Result.Forbidden();
        if (!UserManagementAccess.ValidRoles(command.Roles)) return Result.Invalid(new ValidationError("roles", "Select one or two distinct roles."));
        if (user.Version != command.Version) return Result.Conflict();
        if (user.Id == manager.Value.User.Id && (!command.IsActive || !command.Roles.Contains(TenantRoles.Administrator)))
            return Result.Invalid(new ValidationError("roles", "You cannot deactivate yourself or remove your own Administrator role."));
        var now = clock.GetCurrentInstant();
        var actor = manager.Value.User.KeycloakUserId;
        if (user.FirstName != command.FirstName || user.LastName != command.LastName)
        {
            user.UpdateNames(command.FirstName, command.LastName);
            user.Record(actor, "Name updated", now);
        }
        if (!membership.Roles.Order().SequenceEqual(command.Roles.Order()))
        {
            var previousRoles = string.Join(", ", membership.Roles);
            membership.SetRoles(command.Roles);
            user.Record(actor, "Roles changed", now, $"{previousRoles} → {string.Join(", ", command.Roles)}");
        }
        if (user.IsActive != command.IsActive)
        {
            user.SetActive(command.IsActive);
            user.Record(actor, command.IsActive ? "Reactivated" : "Deactivated", now);
        }
        return true;
    }
}

public sealed class ResetUserPasswordHandler(IRepository repository, UserManagementAccess access,
    IIdentityAdministration identity, IClock clock)
{
    public async Task<Result<bool>> Handle(ResetUserPasswordCommand command, CancellationToken ct)
    {
        var manager = await access.Manager(ct);
        if (manager is null) return Result.Forbidden();
        var user = await repository.GetByIdAsync<UserAggregate, Guid>(command.Id, ct);
        if (user is null || user.TenantId != manager.Value.User.TenantId) return Result.NotFound();
        var membership = await repository.SingleOrDefaultAsync(new MembershipsSpecification(user.TenantId, user.Id), ct);
        if (membership is null) return Result.NotFound();
        if (!UserManagementAccess.CanManage(manager.Value.Administrator, membership.Roles)) return Result.Forbidden();
        if (!user.IsActive) return Result.Invalid(new ValidationError("user", "Reactivate this User before requesting a password reset."));
        try { await identity.SendPasswordResetAsync(user.KeycloakUserId, ct); }
        catch (IdentityAdministrationException exception)
        {
            return Result.Invalid(new ValidationError("email", exception.Message));
        }
        user.Record(manager.Value.User.KeycloakUserId, "Password reset email sent", clock.GetCurrentInstant());
        return true;
    }
}
