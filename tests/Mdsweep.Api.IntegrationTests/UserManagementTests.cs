using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;
using NodaSystemClock = NodaTime.SystemClock;

namespace Mdsweep.Api.IntegrationTests;

public sealed class UserManagementTests : MdsweepIntegrationTest
{
    private const string ActiveTenantId = "mdsw-eep2-3456";
    private const string OtherTenantId = "tnnt-bbbb-2345";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Invite_user_rejects_an_existing_tenant_member(bool isActive)
    {
        const string email = "existing-member@example.test";

        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var administrator = await db.TenantMemberships.SingleAsync();
            administrator.SetRoles(["Administrator"]);

            var user = UserAggregate.Create("Existing", "Member", "existing-member", email);
            var membership = TenantMembership.Create(
                ActiveTenantId,
                user.Id,
                "Existing Member",
                ["Driver"]
            );
            membership.SetActive(isActive);
            db.Users.Add(user);
            db.TenantMemberships.Add(membership);
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/users/invitations",
            new
            {
                email,
                firstName = "Existing",
                lastName = "Member",
                roles = new[] { "Dispatcher" },
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Contains(
            "already belongs to this Tenant",
            problem.RootElement.GetProperty("errors").GetProperty("email")[0].GetString()
        );

        await using var verificationScope = Application.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.DoesNotContain(await verificationDb.Invitations.ToListAsync(), x => x.Email == email);
    }

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

    [Fact]
    public async Task List_users_returns_only_memberships_and_invitations_from_the_active_tenant()
    {
        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var administrator = await db.TenantMemberships.SingleAsync();
            administrator.SetRoles(["Administrator"]);

            var otherTenant = TenantAggregate.Create(OtherTenantId, "Other Synthetic Tenant");
            var otherUser = UserAggregate.Create(
                "Other",
                "User",
                "other-tenant-user",
                "other-user@example.test"
            );
            db.Tenants.Add(otherTenant);
            db.Users.Add(otherUser);
            db.TenantMemberships.Add(
                TenantMembership.Create(OtherTenantId, otherUser.Id, "Other User", ["Driver"])
            );
            db.Invitations.AddRange(
                CreateInvitation(ActiveTenantId, "active-invitee@example.test"),
                CreateInvitation(OtherTenantId, "other-invitee@example.test")
            );
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        using var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var emails = body
            .RootElement.EnumerateArray()
            .Select(item => item.GetProperty("email").GetString())
            .ToList();
        Assert.Contains("dispatcher@example.test", emails);
        Assert.Contains("active-invitee@example.test", emails);
        Assert.DoesNotContain("other-user@example.test", emails);
        Assert.DoesNotContain("other-invitee@example.test", emails);
        Assert.Equal(2, emails.Count);
    }

    [Fact]
    public async Task Cancel_invitation_returns_not_found_for_another_tenant_and_leaves_it_pending()
    {
        Guid invitationId;

        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var administrator = await db.TenantMemberships.SingleAsync();
            administrator.SetRoles(["Administrator"]);

            db.Tenants.Add(TenantAggregate.Create(OtherTenantId, "Other Synthetic Tenant"));
            var invitation = CreateInvitation(OtherTenantId, "other-invitee@example.test");
            invitationId = invitation.Id;
            db.Invitations.Add(invitation);
            await db.SaveChangesAsync();
        }

        using var client = Application.CreateClient();
        await AddAntiforgeryToken(client);

        using var response = await client.DeleteAsync($"/api/users/invitations/{invitationId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var verificationScope = Application.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invitationStatus = await verificationDb.Invitations
            .Where(invitation => invitation.Id == invitationId)
            .Select(invitation => invitation.Status)
            .SingleAsync();
        Assert.Equal(InvitationStatus.Pending, invitationStatus);
    }

    private async Task<bool> IsMembershipActive(Guid userId)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await db.TenantMemberships.SingleAsync(x => x.UserId == userId)).IsActive;
    }

    private static InvitationAggregate CreateInvitation(string tenantId, string email) =>
        InvitationAggregate.Create(
            tenantId,
            email,
            "Synthetic",
            "Invitee",
            ["Driver"],
            $"token-{email}",
            $"hash-{email}",
            NodaSystemClock.Instance.GetCurrentInstant() + Duration.FromDays(1)
        );
}
