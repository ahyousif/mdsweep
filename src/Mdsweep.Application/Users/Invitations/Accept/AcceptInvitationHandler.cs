using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Security;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invitations.Accept;

public sealed class AcceptInvitationHandler(
    IInvitationRepository repository,
    ICurrentIdentity currentIdentity,
    ITokenService tokenService,
    IClock clock
)
{
    public async Task<(Result Result, OutgoingMessages Messages)> Handle(
        AcceptInvitationCommand command,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(currentIdentity.Subject))
        {
            return (Result.Unauthorized(), []);
        }

        if (string.IsNullOrWhiteSpace(currentIdentity.Email))
        {
            return (
                Result.Invalid(
                    new ValidationError(
                        "identity",
                        "Your email address must be verified before accepting an invitation."
                    )
                ),
                []
            );
        }

        var now = clock.GetCurrentInstant();

        var invitation = await repository.GetPendingByTokenHashAsync(tokenService.Hash(command.Token), now, ct);

        if (invitation is null)
        {
            return (
                Result.Invalid(
                    new ValidationError("token", "This invitation is invalid, expired, cancelled, or already used.")
                ),
                []
            );
        }

        if (!string.Equals(invitation.Email, currentIdentity.Email, StringComparison.OrdinalIgnoreCase))
        {
            return (
                Result.Invalid(
                    new ValidationError(
                        "invitationEmailMismatch",
                        "Sign in with the email address this invitation was sent to."
                    )
                ),
                []
            );
        }

        invitation.Accept(now, currentIdentity.Subject);

        // Preserve the established Wolverine 6.35 workaround used by invitation creation:
        // managed conjoined tenancy does not currently scrape these aggregate events.
        var outgoingMessages = new OutgoingMessages();
        foreach (var domainEvent in invitation.DequeueDomainEvents())
        {
            outgoingMessages.Add(domainEvent);
        }

        return (Result.Success(), outgoingMessages);
    }
}
