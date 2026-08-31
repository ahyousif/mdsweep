namespace Mdsweep.Application.Common.Authorization;

public interface ITenantAccess
{
    Task<IReadOnlyList<TenantMembershipContext>> GetMembershipsAsync(
        string userSubject,
        CancellationToken cancellationToken
    );

    Task<bool> HasRoleAsync(string userSubject, string tenantId, string role, CancellationToken cancellationToken);
}

public sealed record TenantMembershipContext(Guid UserId, string TenantId, string Role);
