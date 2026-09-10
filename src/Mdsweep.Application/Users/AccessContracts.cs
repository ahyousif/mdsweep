using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Domain.Users;

namespace Mdsweep.Application.Users;

// Access also runs before Tenant selection. Identity claims are supplied by the BFF,
// never by request bodies; application authorization remains database-backed.
public interface IUserContext
{
    string Subject { get; }
    string? TenantId { get; }
}

public interface IIdentityAdministration
{
    Task InviteAsync(string organizationId, string email, string firstName, string lastName, CancellationToken ct);
    Task<VerifiedIdentity?> GetVerifiedIdentityAsync(string subject, CancellationToken ct);
    Task<bool> IsOrganizationMemberAsync(string subject, string organizationId, CancellationToken ct);
    Task SendPasswordResetAsync(string subject, CancellationToken ct);
}

public sealed record VerifiedIdentity(string Subject, string Email);

public sealed class IdentityAdministrationException(string message) : Exception(message);

public sealed record UserModel(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string DisplayName,
    string[] Roles,
    bool IsActive,
    int Version
);

public sealed record InvitationModel(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string[] Roles,
    string Status,
    Instant ExpiresAt,
    Instant? SentAt,
    string? DeliveryError,
    int Version
)
{
    public static InvitationModel From(InvitationAggregate invitation, Instant now) =>
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

public sealed record HistoryModel(
    string ActorSubject,
    string ActorName,
    string Action,
    Instant OccurredAt,
    string? Details
);

public sealed record UserManagementModel(UserModel[] Users, InvitationModel[] Invitations, bool IsAdministrator);

public sealed record PendingInvitationModel(Guid Id, string TenantName, string[] Roles, Instant ExpiresAt);

public sealed record ListUsersQuery : IQuery<UserManagementModel>;

public sealed record GetAccessHistoryQuery(Guid Id, bool Invitation) : IQuery<HistoryModel[]>;

public sealed record InviteUserCommand(string Email, string FirstName, string LastName, string[] Roles)
    : ICommand<Guid>;

public sealed record SendInvitationCommand(Guid Id) : ICommand<InvitationModel>;

public sealed record RevokeInvitationCommand(Guid Id) : ICommand<bool>;

public sealed record UpdateUserCommand(Guid Id, string DisplayName, string[] Roles, bool IsActive, int Version)
    : ICommand<bool>;

public sealed record ResetUserPasswordCommand(Guid Id) : ICommand<bool>;

public sealed record GetPendingInvitationsQuery : IQuery<PendingInvitationModel[]>;

public sealed record AcceptInvitationCommand(Guid Id) : ICommand<bool>;
