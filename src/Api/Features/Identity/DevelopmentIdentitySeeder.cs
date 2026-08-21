using Microsoft.EntityFrameworkCore;
using Mdsweep.Api.Infrastructure;

namespace Mdsweep.Api.Features.Identity;

internal static class DevelopmentIdentitySeeder
{
    private const string DispatcherSubject = "d4ba70d7-6173-4ad0-9b48-59aa2c6a322a";
    private const string ProviderOrganizationId = "b6d8ea17-b31c-45c0-b5f2-4fec5df7c6cf";

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var provider = await db.Providers.SingleOrDefaultAsync(
            x => x.KeycloakOrganizationId == ProviderOrganizationId, cancellationToken);
        if (provider is null)
        {
            provider = new Provider
            {
                Name = "Synthetic Provider",
                KeycloakOrganizationId = ProviderOrganizationId
            };
            db.Providers.Add(provider);
        }

        var appUser = await db.AppUsers.SingleOrDefaultAsync(
            x => x.KeycloakSubject == DispatcherSubject, cancellationToken);
        if (appUser is null)
        {
            appUser = new AppUser { KeycloakSubject = DispatcherSubject };
            db.AppUsers.Add(appUser);
        }

        if (!await db.ProviderMemberships.AnyAsync(
                x => x.ProviderId == provider.Id && x.AppUserId == appUser.Id, cancellationToken))
        {
            db.ProviderMemberships.Add(new ProviderMembership
            {
                ProviderId = provider.Id,
                AppUserId = appUser.Id,
                Role = "Dispatcher"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
