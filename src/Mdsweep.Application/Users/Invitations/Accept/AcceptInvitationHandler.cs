using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invitations.Accept;

public sealed class AcceptInvitationHandler(
    IInvitationRepository invitationRepository,
    IRepository repository,
    ICurrentIdentity currentIdentity,
    ITokenService tokenService,
    IClock clock
)
{
    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentIdentity.Subject))
        {
            return Result.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(currentIdentity.Email))
        {
            return Result.Invalid(
                new ValidationError("identity", "Your account does not provide an email address.")
            );
        }

        var now = clock.GetCurrentInstant();

        var invitation = await invitationRepository.GetPendingByTokenHashAsync(
            tokenService.Hash(command.Token),
            now,
            ct
        );

        if (invitation is null)
        {
            return Result.Invalid(
                new ValidationError("token", "This invitation is invalid, expired, cancelled, or already used.")
            );
        }

        if (!string.Equals(invitation.Email, currentIdentity.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Invalid(
                new ValidationError(
                    "invitationEmailMismatch",
                    "Sign in with the email address this invitation was sent to."
                )
            );
        }

        var user = await repository.SingleOrDefaultAsync(
            new UsersSpecification().WithKeycloakUserId(currentIdentity.Subject).Build(),
            ct
        );

        if (user is null)
        {
            user = UserAggregate.Create(
                invitation.FirstName,
                invitation.LastName,
                currentIdentity.Subject,
                invitation.Email
            );

            await repository.AddAsync(user, ct);
        }

        var membership = await repository.SingleOrDefaultAsync(
            new MembershipsSpecification().WithTenantId(invitation.TenantId).WithUserId(user.Id).Build(),
            ct
        );

        if (membership is not null)
        {
            return Result.Invalid(
                new ValidationError(
                    "membership",
                    "This user already belongs to this Tenant. Edit or re-enable their access instead."
                )
            );
        }

        invitation.Accept(now);

        membership = TenantMembership.Create(
            invitation.TenantId,
            user.Id,
            $"{invitation.FirstName} {invitation.LastName}",
            invitation.Roles
        );

        await repository.AddAsync(membership, ct);

        return Result.Success();
    }
}
