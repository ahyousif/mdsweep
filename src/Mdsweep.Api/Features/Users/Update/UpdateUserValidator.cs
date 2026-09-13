namespace Mdsweep.Api.Features.Users.Update;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Roles)
            .NotEmpty()
            .Must(roles => roles.Distinct().Count() == roles.Length && roles.All(TenantRoles.All.Contains))
            .WithMessage("Select at least one role. Roles must be distinct and valid.");
    }
}
