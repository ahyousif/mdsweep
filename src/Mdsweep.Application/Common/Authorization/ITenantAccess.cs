namespace Mdsweep.Application.Common.Authorization;

public interface ITenantAccess
{
    Task<bool> HasRoleAsync(string userSubject, string tenantId, string role, CancellationToken cancellationToken);
}
