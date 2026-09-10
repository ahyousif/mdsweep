namespace Mdsweep.Application.Users.Pending;

public sealed record PendingInvitationModel(Guid Id, string TenantName, string[] Roles, Instant ExpiresAt);
