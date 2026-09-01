namespace Mdsweep.Api.Common.Authorization;

public sealed record TenantRoleRequirement(string Role) : IAuthorizationRequirement;
