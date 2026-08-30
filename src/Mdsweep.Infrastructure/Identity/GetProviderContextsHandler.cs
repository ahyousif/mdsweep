using Mdsweep.Application.Identity;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mdsweep.Infrastructure.Identity;

public static class GetProviderContextsHandler
{
    public static Task<List<ProviderContext>> Handle(
        GetProviderContexts query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        (
            from appUser in db.AppUsers
            join membership in db.ProviderMemberships on appUser.Id equals membership.AppUserId
            where
                appUser.KeycloakSubject == query.Subject
                && (query.ProviderId == null || membership.ProviderId == query.ProviderId)
            select new ProviderContext(membership.ProviderId, appUser.Id, membership.Role)
        ).ToListAsync(cancellationToken);
}
