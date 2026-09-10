namespace Mdsweep.Api.Features.Users.Invitations.Invite;

public sealed class InviteUserValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Roles)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(roles => roles.Length <= 2)
            .WithMessage("Select no more than two roles.")
            .Must(roles => roles.Distinct().Count() == roles.Length)
            .WithMessage("Roles must be distinct.");

        RuleForEach(x => x.Roles).Must(TenantRoles.All.Contains).WithMessage("Invalid role.");
    }
}
