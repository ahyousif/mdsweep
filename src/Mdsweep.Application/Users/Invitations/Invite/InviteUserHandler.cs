using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.Invitations.Invite;

public sealed class InviteUserHandler(IRepository repository, ITokenService tokenService, IClock clock)
{
    public async Task<Result> Handle(InviteUserCommand command, TenantId tenantId, CancellationToken ct)
    {
        var now = clock.GetCurrentInstant();
        var email = command.Email.Trim().ToLowerInvariant();

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
            now + Duration.FromDays(7)
        );

        await repository.AddAsync(invitation, ct);

        return Result.Success();
    }
}
