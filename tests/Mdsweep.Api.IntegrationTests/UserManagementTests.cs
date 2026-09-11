using System.Net;
using System.Net.Http.Json;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.IntegrationTests;

public sealed class UserManagementTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Update_user_route_disables_and_reenables_the_same_membership()
    {
        Guid userId;

        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var administrator = await db.TenantMemberships.SingleAsync();
            administrator.SetRoles(["Administrator"]);

            var user = UserAggregate.Create(
                "Synthetic",
                "Driver",
                "synthetic-driver",
                "synthetic-driver@example.test"
            );
            userId = user.Id;
            db.Users.Add(user);
            db.TenantMemberships.Add(
                TenantMembership.Create("mdsw-eep2-3456", user.Id, "Synthetic Driver", ["Driver"])
            );
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var disableResponse = await client.PutAsJsonAsync(
            $"/api/users/{userId}",
            new { displayName = "Synthetic Driver", roles = new[] { "Driver" }, isActive = false }
        );

        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);
        Assert.False(await IsMembershipActive(userId));

        using var enableResponse = await client.PutAsJsonAsync(
            $"/api/users/{userId}",
            new { displayName = "Synthetic Driver", roles = new[] { "Driver" }, isActive = true }
        );

        Assert.Equal(HttpStatusCode.NoContent, enableResponse.StatusCode);
        Assert.True(await IsMembershipActive(userId));
    }

    private async Task<bool> IsMembershipActive(Guid userId)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await db.TenantMemberships.SingleAsync(x => x.UserId == userId)).IsActive;
    }
}
