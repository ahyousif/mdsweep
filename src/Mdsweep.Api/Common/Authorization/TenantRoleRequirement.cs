namespace Mdsweep.Api.Common.Authorization;

public sealed record TenantRoleRequirement(params string[] Roles) : IAuthorizationRequirement;
