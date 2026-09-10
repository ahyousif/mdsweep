using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Domain.Users.Events;

namespace Mdsweep.Application.Users.DomainEventHandlers;

public sealed class CreateUserWhenInvitationAcceptedHandler(IRepository repository)
{
    public async Task Handle(InvitationAcceptedDomainEvent @event, CancellationToken ct)
    {
        var user = await repository.SingleOrDefaultAsync(
            new UsersSpecification().WithKeycloakUserId(@event.KeycloakUserId).Build(),
            ct
        );

        if (user is null)
        {
            user = UserAggregate.Create(@event.FirstName, @event.LastName, @event.KeycloakUserId, @event.Email);

            await repository.AddAsync(user, ct);
        }

        var membership = await repository.SingleOrDefaultAsync(
            new MembershipsSpecification().WithTenantId(@event.TenantId).WithUserId(user.Id).Build(),
            ct
        );

        if (membership is not null)
        {
            return;
        }

        membership = TenantMembership.Create(
            @event.TenantId,
            user.Id,
            displayName: $"{@event.FirstName} {@event.LastName}",
            @event.Roles
        );

        await repository.AddAsync(membership, ct);
    }
}
