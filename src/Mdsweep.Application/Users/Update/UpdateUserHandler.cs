using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Update;

public sealed class UpdateUserHandler(IRepository repository, ICurrentIdentity currentIdentity)
{
    public async Task<Result> Handle(UpdateUserCommand command, TenantId tenantId, CancellationToken ct)
    {
        var membership = await repository.SingleOrDefaultAsync(
            new MembershipsSpecification().WithTenantId(tenantId.Value).WithUserId(command.UserId).Build(),
            ct
        );

        if (membership is null)
        {
            return Result.NotFound();
        }

        var user = await repository.GetByIdAsync<UserAggregate, Guid>(command.UserId, ct);

        if (user is null)
        {
            return Result.NotFound();
        }

        if (
            user.KeycloakUserId == currentIdentity.Subject
            && (!command.IsActive || !command.Roles.Contains(TenantRoles.Administrator))
        )
        {
            return Result.Invalid(
                new ValidationError("user", "You cannot deactivate yourself or remove your own Administrator role.")
            );
        }

        membership.SetDisplayName(command.DisplayName);
        membership.SetRoles(command.Roles);
        membership.SetActive(command.IsActive);

        return Result.Success();
    }
}
