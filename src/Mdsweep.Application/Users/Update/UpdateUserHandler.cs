using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Update;

public sealed class UpdateUserHandler(IRepository repository, IUserContext context)
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

        if ((membership.DisplayName ?? $"{user.FirstName} {user.LastName}") != command.DisplayName)
        {
            membership.SetDisplayName(command.DisplayName);
        }
        if (!membership.Roles.Order().SequenceEqual(command.Roles.Order()))
        {
            membership.SetRoles(command.Roles);
        }
        if (membership.IsActive != command.IsActive)
        {
            membership.SetActive(command.IsActive);
        }
        return true;
    }
}
