using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class ListUsersHandler(IRepository repository, IUserContext context, IClock clock)
{
    public async Task<Result<UserManagementModel>> Handle(ListUsersQuery query, CancellationToken ct)
    {
        if (context.TenantId is null)
            return Result.Forbidden();
        var memberships = await repository.ListAsync(new MembershipsSpecification(context.TenantId), ct);
        var users = await repository.ListAsync(
            new UsersSpecification(userIds: memberships.Select(x => x.UserId).ToArray()),
            ct
        );
        var invitations = await repository.ListAsync(new InvitationsSpecification(context.TenantId), ct);
        var models = users
            .Join(
                memberships,
                x => x.Id,
                x => x.UserId,
                (user, membership) =>
                    new UserModel(
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        membership.DisplayName ?? $"{user.FirstName} {user.LastName}",
                        membership.Roles,
                        membership.IsActive,
                        membership.Version
                    )
            )
            .ToArray();
        return new UserManagementModel(
            models,
            invitations.Select(x => InvitationModel.From(x, clock.GetCurrentInstant())).ToArray(),
            true
        );
    }
}
