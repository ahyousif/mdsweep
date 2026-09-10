using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.ResetPassword;

public sealed class ResetUserPasswordHandler(
    IRepository repository,
    IUserContext context,
    IIdentityAdministration identity
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

        return true;
    }
}
