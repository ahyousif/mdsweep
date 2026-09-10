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

public sealed class GetAccessHistoryHandler(IRepository repository, IUserContext context)
{
    public async Task<Result<HistoryModel[]>> Handle(GetAccessHistoryQuery query, CancellationToken ct)
    {
        if (context.TenantId is null)
            return Result.Forbidden();
        IReadOnlyCollection<AccessHistoryEntry> history;
        if (query.Invitation)
        {
            var invitation = await repository.GetByIdAsync<InvitationAggregate, Guid>(query.Id, ct);
            if (invitation is null || invitation.TenantId != context.TenantId)
                return Result.NotFound();
            history = invitation.History;
        }
        else
        {
            var user = await repository.GetByIdAsync<UserAggregate, Guid>(query.Id, ct);
            if (user is null)
                return Result.NotFound();
            var membership = await repository.SingleOrDefaultAsync(
                new MembershipsSpecification(context.TenantId, user.Id),
                ct
            );
            if (membership is null)
                return Result.NotFound();
            history = membership.History;
        }
        var memberships = await repository.ListAsync(new MembershipsSpecification(context.TenantId), ct);
        var users = await repository.ListAsync(
            new UsersSpecification(userIds: memberships.Select(x => x.UserId).ToArray()),
            ct
        );
        var names = users.ToDictionary(x => x.KeycloakUserId, x => $"{x.FirstName} {x.LastName}");
        return history
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new HistoryModel(
                x.ActorSubject,
                names.GetValueOrDefault(x.ActorSubject, "Former User"),
                x.Action,
                x.OccurredAt,
                x.Details
            ))
            .ToArray();
    }
}
