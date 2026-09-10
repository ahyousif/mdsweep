using Mdsweep.Domain.Tenants;

namespace Mdsweep.Api.Features.Users.Invite;

public sealed class InviteUserValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254)
            .Must(x => x is null || x == x.Trim())
            .WithMessage("Email must not have leading or trailing spaces.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Roles)
            .Must(TenantMembership.AreValidRoles)
            .WithMessage("Select one or two distinct roles: Administrator, Dispatcher, or Driver.");
    }
}
