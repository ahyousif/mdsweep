namespace Mdsweep.Api.Features.Users.Update;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithErrorCode("displayNameRequired")
            .MaximumLength(100)
            .WithErrorCode("displayNameTooLong");

        RuleFor(x => x.Roles)
            .NotEmpty()
            .WithErrorCode("rolesInvalid")
            .Must(roles =>
                roles.Length <= 2 && roles.Distinct().Count() == roles.Length && roles.All(TenantRoles.All.Contains)
            )
            .WithMessage("Select one or two distinct valid roles.")
            .WithErrorCode("rolesInvalid");
    }
}
