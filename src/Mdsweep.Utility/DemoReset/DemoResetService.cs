using Mdsweep.Application.Common.Configuration;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Mdsweep.Utility.DemoReset;

public sealed class DemoResetService(
    ApplicationDbContext db,
    KeycloakAdminClient keycloak,
    IOptions<WebOptions> webOptions
)
{
    private const string TenantId = "demo-env-0001";
    private const string Email = "demo.admin@mdsweep.test";

    public Task<string> RecreateKeycloakRealmAsync(CancellationToken cancellationToken = default) =>
        keycloak.RecreateDemoRealmAsync(webOptions.Value.BaseUrl, cancellationToken);

    public async Task CreateApplicationDataAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        var tenant = TenantAggregate.Create(TenantId, "MDSweep Demo");
        var user = UserAggregate.Create("Demo", "Administrator", keycloakUserId, Email);
        var membership = TenantMembership.Create(tenant.Id, user.Id, "Demo Administrator", ["Administrator"]);

        db.Tenants.Add(tenant);
        db.Users.Add(user);
        db.TenantMemberships.Add(membership);
        await db.SaveChangesAsync(cancellationToken);
    }
}
