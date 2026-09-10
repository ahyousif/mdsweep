using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users.List;

public sealed record UserListItem(
    Guid Id,
    UserListItemType Type,
    string FirstName,
    string LastName,
    string Email,
    string DisplayName,
    string[] Roles,
    UserListItemStatus Status
)
{
    public static UserListItem FromUser(UserAggregate user, TenantMembership membership) =>
        new(
            user.Id,
            UserListItemType.User,
            user.FirstName,
            user.LastName,
            user.Email,
            membership.DisplayName ?? $"{user.FirstName} {user.LastName}",
            membership.Roles,
            membership.IsActive ? UserListItemStatus.Active : UserListItemStatus.Inactive
        );

    public static UserListItem FromInvitation(InvitationAggregate invitation) =>
        new(
            invitation.Id,
            UserListItemType.Invitation,
            invitation.FirstName,
            invitation.LastName,
            invitation.Email,
            $"{invitation.FirstName} {invitation.LastName}",
            invitation.Roles,
            UserListItemStatus.Invited
        );
}
