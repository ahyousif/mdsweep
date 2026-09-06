using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class ListUsersHandler(IRepository repository, UserManagementAccess access, IClock clock)
{
    public async Task<Result<UserManagementModel>> Handle(ListUsersQuery query, CancellationToken ct)
    {
        var manager = await access.Manager(ct);
        if (manager is null) return Result.Forbidden();
        var (actor, administrator) = manager.Value;
        var users = await repository.ListAsync(new UsersSpecification(actor.TenantId), ct);
        var memberships = await repository.ListAsync(new MembershipsSpecification(actor.TenantId), ct);
        var invitations = await repository.ListAsync(new InvitationsSpecification(actor.TenantId), ct);
        var models = users.Join(memberships, x => x.Id, x => x.UserId,
            (user, membership) => new UserModel(user.Id, user.FirstName, user.LastName, user.Email,
                membership.Roles, user.IsActive, user.Version))
            .Where(x => UserManagementAccess.CanManage(administrator, x.Roles)).ToArray();
        return new UserManagementModel(models, invitations
            .Where(x => UserManagementAccess.CanManage(administrator, x.Roles))
            .Select(x => UserManagementAccess.Model(x, clock.GetCurrentInstant())).ToArray(), administrator);
    }
}

public sealed class GetAccessHistoryHandler(IRepository repository, UserManagementAccess access)
{
    public async Task<Result<HistoryModel[]>> Handle(GetAccessHistoryQuery query, CancellationToken ct)
    {
        var manager = await access.Manager(ct);
        if (manager is null) return Result.Forbidden();
        IReadOnlyCollection<AccessHistoryEntry> history;
        string[] roles;
        if (query.Invitation)
        {
            var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(query.Id, ct);
            if (invitation is null || invitation.TenantId != manager.Value.User.TenantId) return Result.NotFound();
            history = invitation.History;
            roles = invitation.Roles;
        }
        else
        {
            var user = await repository.GetByIdAsync<UserAggregate, Guid>(query.Id, ct);
            if (user is null || user.TenantId != manager.Value.User.TenantId) return Result.NotFound();
            history = user.History;
            var membership = await repository.SingleOrDefaultAsync(new MembershipsSpecification(user.TenantId, user.Id), ct);
            if (membership is null) return Result.NotFound();
            roles = membership.Roles;
        }
        if (!UserManagementAccess.CanManage(manager.Value.Administrator, roles)) return Result.Forbidden();
        var users = await repository.ListAsync(new UsersSpecification(manager.Value.User.TenantId), ct);
        var names = users.ToDictionary(x => x.KeycloakUserId, x => $"{x.FirstName} {x.LastName}");
        return history.OrderByDescending(x => x.OccurredAt)
            .Select(x => new HistoryModel(x.ActorSubject, names.GetValueOrDefault(x.ActorSubject, "Former User"),
                x.Action, x.OccurredAt, x.Details)).ToArray();
    }
}
