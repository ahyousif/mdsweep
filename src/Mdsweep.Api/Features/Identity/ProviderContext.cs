using System.Security.Claims;
using Mdsweep.Application.Identity;
using Wolverine;

namespace Mdsweep.Api.Features.Identity;

public static class ProviderContextResolver
{
    public const string ActiveProviderIdClaim = "mdsweep_provider_id";

    public static bool HasRole(ProviderContext? context, string role) =>
        context is not null && string.Equals(context.Role, role, StringComparison.Ordinal);

    public static async Task<IReadOnlyList<ProviderContext>> ResolveAll(
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var subject = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            return [];

        return await bus.InvokeAsync<List<ProviderContext>>(
            new GetProviderContexts(subject),
            cancellationToken
        );
    }

    public static async Task<ProviderContext?> ResolveActive(
        ClaimsPrincipal user,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var subject = user.FindFirstValue("sub");
        var providerId = user.FindFirstValue(ActiveProviderIdClaim);
        if (string.IsNullOrWhiteSpace(subject) || !Guid.TryParse(providerId, out var id))
            return null;

        return (
            await bus.InvokeAsync<List<ProviderContext>>(
                new GetProviderContexts(subject, id),
                cancellationToken
            )
        ).SingleOrDefault();
    }
}
