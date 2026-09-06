using Mdsweep.Application.Users;

namespace Mdsweep.Api.Features.Users;

public sealed record InviteUserRequest(string Email, string FirstName, string LastName, string[] Roles);
public sealed record UpdateUserRequest(string FirstName, string LastName, string[] Roles, bool IsActive, int Version);

public sealed class InviteUserValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254)
            .Must(x => x is null || x == x.Trim()).WithMessage("Email must not have leading or trailing spaces.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Roles).Must(UserManagementAccess.ValidRoles).WithMessage("Select one or two distinct roles: Administrator, Dispatcher, or Driver.");
    }
}

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Roles).Must(UserManagementAccess.ValidRoles).WithMessage("Select one or two distinct roles: Administrator, Dispatcher, or Driver.");
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
    }
}
