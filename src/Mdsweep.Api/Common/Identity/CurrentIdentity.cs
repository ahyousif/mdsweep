using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Api.Common.Identity;

public sealed class CurrentIdentity(IHttpContextAccessor httpContextAccessor) : ICurrentIdentity
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string Subject => User?.FindFirstValue("sub") ?? string.Empty;

    public string? Email => User?.FindFirstValue("email");
}
