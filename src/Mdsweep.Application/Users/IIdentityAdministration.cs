namespace Mdsweep.Application.Users;

public interface IIdentityAdministration
{
    Task InviteAsync(string organizationId, string email, string firstName, string lastName, CancellationToken ct);
    Task<VerifiedIdentity?> GetVerifiedIdentityAsync(string subject, CancellationToken ct);
    Task<bool> IsOrganizationMemberAsync(string subject, string organizationId, CancellationToken ct);
    Task SendPasswordResetAsync(string subject, CancellationToken ct);
}
