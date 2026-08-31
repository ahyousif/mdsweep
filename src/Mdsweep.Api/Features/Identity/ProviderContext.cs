using System.Security.Claims;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.Features.Identity;

public sealed record ProviderContext(Guid ProviderId, Guid AppUserId, string Role);

public static class ProviderContextResolver
{
    public const string ActiveProviderIdClaim = "mdsweep_provider_id";

    public static bool HasRole(ProviderContext? context, string role) =>
        context is not null && string.Equals(context.Role, role, StringComparison.Ordinal);

    public static async Task<IReadOnlyList<ProviderContext>> ResolveAll(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var subject = user.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
            return [];

        return await (
            from appUser in db.AppUsers
            join membership in db.ProviderMemberships on appUser.Id equals membership.AppUserId
            where appUser.KeycloakSubject == subject
            select new ProviderContext(membership.ProviderId, appUser.Id, membership.Role)
        ).ToListAsync(cancellationToken);
    }

    public static async Task<ProviderContext?> ResolveActive(
        ClaimsPrincipal user,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    )
    {
        var providerId = user.FindFirstValue(ActiveProviderIdClaim);
        return Guid.TryParse(providerId, out var id)
            ? (await ResolveAll(user, db, cancellationToken)).SingleOrDefault(x =>
                x.ProviderId == id
            )
            : null;
    }
}
