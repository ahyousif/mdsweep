using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Specifications;
using Mdsweep.Application.Users.Specifications;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.List;

public sealed class ListUsersHandler(IRepository repository)
{
    public async Task<Result<IReadOnlyList<UserListItem>>> Handle(ListUsersQuery _, CancellationToken ct)
    {
        var memberships = await repository.ListAsync(new MembershipsSpecification().AsNoTracking().Build(), ct);

        var users = await repository.ListAsync(
            new UsersSpecification().WithIds(memberships.Select(x => x.UserId)).AsNoTracking().Build(),
            ct
        );

        var invitations = await repository.ListAsync(
            new InvitationsSpecification().WithStatus(InvitationStatus.Pending).AsNoTracking().Build(),
            ct
        );

        var userItems = users.Join(
            memberships,
            user => user.Id,
            membership => membership.UserId,
            UserListItem.FromUser
        );

        var items = userItems
            .Concat(invitations.Select(UserListItem.FromInvitation))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();

        return Result.Success<IReadOnlyList<UserListItem>>(items);
    }
}
