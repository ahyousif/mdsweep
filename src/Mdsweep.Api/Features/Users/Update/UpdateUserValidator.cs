using Mdsweep.Domain.Tenants;

namespace Mdsweep.Api.Features.Users.Update;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(401);
        RuleFor(x => x.Roles)
            .Must(TenantMembership.AreValidRoles)
            .WithMessage("Select one or two distinct roles: Administrator, Dispatcher, or Driver.");
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
    }
}
