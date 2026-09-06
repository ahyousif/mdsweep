using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Domain.Users;
using Mdsweep.Domain.Tenants;

namespace Mdsweep.Application.Users;

public sealed class UserManagementAccess(IRepository repository, IAccessActor actor)
{
    public async Task<(UserAggregate User, bool Administrator)?> Manager(CancellationToken ct)
    {
        if (actor.TenantId is null) return null;
        var user = await repository.SingleOrDefaultAsync(new UsersSpecification(actor.TenantId, actor.Subject), ct);
        if (user is null || !user.IsActive) return null;
        var roles = await repository.ListAsync(new MembershipsSpecification(user.TenantId, user.Id), ct);
        if (!roles.Any(x => x.Roles.Any(role => role is TenantRoles.Administrator or TenantRoles.Dispatcher))) return null;
        return (user, roles.Any(x => x.Roles.Contains(TenantRoles.Administrator)));
    }

    public static bool CanManage(bool administrator, string[] roles) =>
        administrator || roles is [TenantRoles.Driver];

    public static bool ValidRoles(string[]? roles) => TenantMembership.AreValidRoles(roles);

    public static InvitationModel Model(InvitationAggregate invitation, Instant now) => new(
        invitation.Id, invitation.FirstName, invitation.LastName, invitation.Email, invitation.Roles,
        invitation.Status == "Pending" && invitation.ExpiresAt <= now ? "Expired" : invitation.Status,
        invitation.ExpiresAt, invitation.SentAt, invitation.DeliveryError, invitation.Version);
}
