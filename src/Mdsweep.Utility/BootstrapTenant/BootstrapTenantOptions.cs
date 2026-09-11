namespace Mdsweep.Utility.BootstrapTenant;

public sealed record BootstrapTenantOptions(
    string TenantId,
    string TenantName,
    string KeycloakUserId,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName
);
