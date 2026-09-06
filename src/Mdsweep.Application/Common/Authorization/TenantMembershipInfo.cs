namespace Mdsweep.Application.Common.Authorization;

public sealed record TenantMembershipInfo(
    Guid UserId,
    string FirstName,
    string LastName,
    string TenantId,
    string TenantName,
    string Role
);
