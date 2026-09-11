using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invitations.Resend;

public sealed class ResendInvitationHandler(IRepository repository, ITokenService tokenService, IClock clock)
{
    public async Task<(Result Result, OutgoingMessages Messages)> Handle(
        ResendInvitationCommand command,
        TenantId tenantId,
        CancellationToken ct
    )
    {
        var invitation = await repository.SingleOrDefaultAsync(
            new InvitationsSpecification()
                .WithId(command.Id)
                .WithTenantId(tenantId.Value)
                .WithStatus(InvitationStatus.Pending)
                .Build(),
            ct
        );

        if (invitation is null)
        {
            return (Result.NotFound(), new OutgoingMessages());
        }

        var token = tokenService.Generate();
        invitation.Resend(token.Value, token.Hash, clock.GetCurrentInstant() + Duration.FromDays(7));

        // Temporary workaround for Wolverine 6.35.0: Lightweight EF transactions do not scrape
        // aggregate domain events with managed conjoined tenancy and a DbContext abstraction.
        // Remove when upstream support is fixed.
        var outgoingMessages = new OutgoingMessages();

        foreach (var domainEvent in invitation.DequeueDomainEvents())
        {
            outgoingMessages.Add(domainEvent);
        }

        return (Result.Success(), outgoingMessages);
    }
}
