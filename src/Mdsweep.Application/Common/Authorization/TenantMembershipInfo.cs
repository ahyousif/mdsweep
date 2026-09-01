namespace Mdsweep.Application.Common.Authorization;

public sealed record TenantMembershipInfo(Guid UserId, string TenantId, string Role);
