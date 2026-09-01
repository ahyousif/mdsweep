namespace Mdsweep.Application.Common.Authorization;

public interface ITenantAccess
{
    Task<IReadOnlyList<TenantMembershipInfo>> GetMembershipsAsync(
        string userSubject,
        CancellationToken cancellationToken
    );

    Task<bool> HasRoleAsync(string userSubject, string tenantId, string role, CancellationToken cancellationToken);
}
