using System.Security.Claims;
using Mdsweep.Api.Common.Authentication;
using Mdsweep.Application.Users;

namespace Mdsweep.Api.Features.Users;

public sealed class HttpAccessActor(IHttpContextAccessor context) : IAccessActor
{
    public string Subject => context.HttpContext?.User.FindFirstValue("sub") ?? string.Empty;
    public string? TenantId => context.HttpContext?.User.FindFirstValue(CustomClaimTypes.ActiveTenantId);
}
