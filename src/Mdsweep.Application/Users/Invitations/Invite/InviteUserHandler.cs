using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invitations.Invite;

public sealed class InviteUserHandler(IRepository repository, ITokenService tokenService, IClock clock)
{
    public async Task<(Result Result, OutgoingMessages Messages)> Handle(
        InviteUserCommand command,
        TenantId tenantId,
        CancellationToken ct
    )
    {
        var now = clock.GetCurrentInstant();
        var email = command.Email.Trim().ToLowerInvariant();

        var user = await repository.SingleOrDefaultAsync(new UsersSpecification().WithEmail(email).Build(), ct);

        if (user is not null)
        {
            var membership = await repository.SingleOrDefaultAsync(
                new MembershipsSpecification().WithTenantId(tenantId.Value).WithUserId(user.Id).Build(),
                ct
            );

            if (membership is not null)
            {
                return (
                    Result.Invalid(
                        new ValidationError(
                            "email",
                            "This user already belongs to this Tenant. Edit or re-enable their access instead."
                        )
                    ),
                    new OutgoingMessages()
                );
            }
        }

        var existing = await repository.SingleOrDefaultAsync(
            new InvitationsSpecification()
                .WithEmail(email)
                .WithStatus(InvitationStatus.Pending)
                .WithTenantId(tenantId.Value)
                .Build(),
            ct
        );

        if (existing is not null)
        {
            existing.Cancel();
        }

        var token = tokenService.Generate();

        var invitation = InvitationAggregate.Create(
            tenantId.Value,
            email,
            command.FirstName,
            command.LastName,
            command.Roles,
            token.Value,
            token.Hash,
            now + Duration.FromDays(7),
            command.DisplayName
        );

        await repository.AddAsync(invitation, ct);

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
