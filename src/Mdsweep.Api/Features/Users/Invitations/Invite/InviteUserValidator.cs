namespace Mdsweep.Api.Features.Users.Invitations.Invite;

public sealed class InviteUserValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode("emailRequired")
            .EmailAddress()
            .WithErrorCode("emailInvalid")
            .MaximumLength(200)
            .WithErrorCode("emailTooLong");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithErrorCode("firstNameRequired")
            .MaximumLength(100)
            .WithErrorCode("firstNameTooLong");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithErrorCode("lastNameRequired")
            .MaximumLength(100)
            .WithErrorCode("lastNameTooLong");

        RuleFor(x => x.Roles)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("rolesInvalid")
            .Must(roles => roles.Length <= 2)
            .WithMessage("Select no more than two roles.")
            .WithErrorCode("rolesInvalid")
            .Must(roles => roles.Distinct().Count() == roles.Length)
            .WithMessage("Roles must be distinct.")
            .WithErrorCode("rolesInvalid");

        RuleForEach(x => x.Roles)
            .Must(TenantRoles.All.Contains)
            .WithMessage("Invalid role.")
            .WithErrorCode("rolesInvalid");
    }
}
