using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

public sealed class UserManagementAccess(IRepository repository, IAccessActor actor)
{
    public async Task<(UserAggregate User, TenantMembership Membership, bool Administrator)?> Manager(
        CancellationToken ct
    )
    {
        if (actor.TenantId is null)
            return null;
        var user = await repository.SingleOrDefaultAsync(new UsersSpecification(subject: actor.Subject), ct);
        if (user is null)
            return null;
        var membership = await repository.SingleOrDefaultAsync(
            new MembershipsSpecification(actor.TenantId, user.Id),
            ct
        );
        if (
            membership is null
            || !membership.IsActive
            || !membership.Roles.Any(role => role is TenantRoles.Administrator or TenantRoles.Dispatcher)
        )
            return null;
        return (user, membership, membership.Roles.Contains(TenantRoles.Administrator));
    }

    public static bool CanManage(bool administrator, string[] roles) => administrator || roles is [TenantRoles.Driver];

    public static bool ValidRoles(string[]? roles) => TenantMembership.AreValidRoles(roles);

    public static InvitationModel Model(InvitationAggregate invitation, Instant now) =>
        new(
            invitation.Id,
            invitation.FirstName,
            invitation.LastName,
            invitation.Email,
            invitation.Roles,
            invitation.Status == "Pending" && invitation.ExpiresAt <= now ? "Expired" : invitation.Status,
            invitation.ExpiresAt,
            invitation.SentAt,
            invitation.DeliveryError,
            invitation.Version
        );
}
