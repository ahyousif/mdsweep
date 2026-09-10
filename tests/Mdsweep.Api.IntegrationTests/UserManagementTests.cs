using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Application.Users;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Mdsweep.Api.IntegrationTests;

public sealed class UserManagementTests : MdsweepIntegrationTest
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(
        JsonSerializerDefaults.Web
    ).ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    private const string TenantId = "mdsw-eep2-3456";
    private TestKeycloakUserAdministration Identity =>
        Application.Services.GetRequiredService<TestKeycloakUserAdministration>();

    [Fact]
    public async Task Concurrent_invitations_do_not_duplicate_the_recipient()
    {
        using var manager = await Client();
        var request = new
        {
            email = "driver@example.test",
            firstName = "Synthetic",
            lastName = "Driver",
            roles = new[] { "Driver" },
        };
        var responses = await Task.WhenAll(
            manager.PostAsJsonAsync("/api/users/invitations", request),
            manager.PostAsJsonAsync("/api/users/invitations", request)
        );
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
        Assert.All(
            responses,
            x =>
                Assert.Contains(
                    x.StatusCode,
                    new[] { HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.BadRequest }
                )
        );
        await using var scope = Application.Services.CreateAsyncScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Invitations.ToListAsync());
    }

    [Fact]
    public async Task Invitation_acceptance_creates_one_User_and_role_and_retains_history()
    {
        using var manager = await Client();
        var invitation = await Invite(manager);
        await using (var scope = Application.Services.CreateAsyncScope())
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync());
        using var recipient = await Client("invited-subject");
        var pending = await recipient.GetFromJsonAsync<PendingInvitationModel[]>("/api/invitation", Json);
        Assert.Equal(invitation.Id, Assert.Single(pending!).Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await recipient.GetAsync("/api/users")).StatusCode);
        (await recipient.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).EnsureSuccessStatusCode();
        (await recipient.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).EnsureSuccessStatusCode();
        await using var scope2 = Application.Services.CreateAsyncScope();
        var db = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(x => x.KeycloakUserId == "invited-subject");
        Assert.True((await db.TenantMemberships.SingleAsync(x => x.UserId == user.Id)).IsActive);
        Assert.Equal(TenantId, (await db.TenantMemberships.SingleAsync(x => x.UserId == user.Id)).TenantId);
        Assert.Equal(new[] { "Driver" }, (await db.TenantMemberships.SingleAsync(x => x.UserId == user.Id)).Roles);
        Assert.Single((await db.TenantMemberships.SingleAsync(x => x.UserId == user.Id)).History);
        Assert.Equal("Accepted", (await db.Invitations.SingleAsync()).Status);
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await recipient.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task Email_failure_is_visible_and_retryable_without_duplicate_invitations()
    {
        Identity.FailEmail = true;
        using var manager = await Client();
        var invitation = await Invite(manager);
        Assert.NotNull(invitation.DeliveryError);
        Assert.Null(invitation.SentAt);
        Assert.Equal("Pending", invitation.Status);
        var duplicate = await manager.PostAsJsonAsync(
            "/api/users/invitations",
            new
            {
                email = "DRIVER@example.test",
                firstName = "Synthetic",
                lastName = "Driver",
                roles = new[] { "Driver" },
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Identity.FailEmail = false;
        var sent = await (
            await manager.PostAsync($"/api/users/invitations/{invitation.Id}/resend", null)
        ).Content.ReadFromJsonAsync<InvitationModel>(Json);
        Assert.NotNull(sent!.SentAt);
        Assert.Null(sent.DeliveryError);
        var history = await manager.GetFromJsonAsync<HistoryModel[]>(
            $"/api/users/invitations/{invitation.Id}/history",
            Json
        );
        Assert.Contains(history!, x => x.Action == "Invitation email failed");
        Assert.Contains(history!, x => x.Action == "Invitation email sent");
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("wrong-email")]
    [InlineData("unverified")]
    [InlineData("not-member")]
    public async Task Unavailable_invitation_or_unverified_identity_cannot_gain_access(string scenario)
    {
        using var manager = await Client();
        var invitation = await Invite(manager);
        if (scenario == "revoked")
            (await manager.PostAsync($"/api/users/invitations/{invitation.Id}/revoke", null)).EnsureSuccessStatusCode();
        if (scenario == "expired")
        {
            await using var scope = Application.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Invitations.ExecuteUpdateAsync(x =>
                x.SetProperty(
                    i => i.ExpiresAt,
                    NodaTime.SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(1)
                )
            );
        }
        if (scenario == "wrong-email")
            Identity.Email = "someone-else@example.test";
        if (scenario == "unverified")
            Identity.Verified = false;
        if (scenario == "not-member")
            Identity.IsMember = false;
        using var recipient = await Client("invited-subject");
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await recipient.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).StatusCode
        );
        await using var scope2 = Application.Services.CreateAsyncScope();
        Assert.Equal(1, await scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.CountAsync());
    }

    [Fact]
    public async Task Dispatcher_cannot_manage_privileged_Users_or_grant_privileged_roles()
    {
        var privileged = await AddUser("Administrator", "admin-subject", "admin@example.test");
        using var manager = await Client("dispatcher-test");
        foreach (var role in new[] { "Administrator", "Dispatcher", "Driver" })
        {
            var response = await manager.PostAsJsonAsync(
                "/api/users/invitations",
                new
                {
                    email = "new@example.test",
                    firstName = "New",
                    lastName = "User",
                    roles = new[] { role },
                }
            );
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await Update(manager, privileged, "Driver")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.PostAsync($"/api/users/{privileged.Id}/password-reset", null)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.GetAsync($"/api/users/{privileged.Id}/history")).StatusCode
        );
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync("/api/users")).StatusCode);
        var driver = await AddUser("Driver", "driver-subject", "driver2@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await Update(manager, driver, "Driver")).StatusCode);
        using var administrator = await Client("admin-subject");
        var privilegedInvitation = await Invite(administrator, "Dispatcher", "new@example.test");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.PostAsync($"/api/users/invitations/{privilegedInvitation.Id}/resend", null)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.PostAsync($"/api/users/invitations/{privilegedInvitation.Id}/revoke", null)).StatusCode
        );
    }

    [Fact]
    public async Task Administrator_can_edit_roles_and_names_and_deactivate_existing_sessions()
    {
        var admin = await AddUser("Administrator", "admin-subject", "admin@example.test");
        var driver = await AddUser("Driver", "driver-subject", "driver2@example.test");
        using var manager = await Client("admin-subject");
        using var target = await Client("driver-subject");
        (await Update(manager, driver, "Dispatcher", displayName: "Updated Driver")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await target.GetAsync("/api/users")).StatusCode);
        var updated = (await manager.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Id == driver.Id
        );
        (await Update(manager, updated, "Dispatcher", false)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await target.GetAsync("/api/users")).StatusCode);
        Assert.Empty(
            (await target.GetFromJsonAsync<JsonElement>("/api/auth/session"))
                .GetProperty("availableTenants")
                .EnumerateArray()
        );
        Assert.Equal(HttpStatusCode.Conflict, (await Update(manager, updated, "Driver")).StatusCode);
        var deactivated = (await manager.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Id == driver.Id
        );
        (await Update(manager, deactivated, "Driver", true)).EnsureSuccessStatusCode();
        var history = await manager.GetFromJsonAsync<HistoryModel[]>($"/api/users/{driver.Id}/history", Json);
        Assert.Contains(history!, x => x.Action == "Display name updated");
        Assert.Contains(history!, x => x.Action == "Deactivated");
        Assert.Contains(history!, x => x.Action == "Reactivated");
        Assert.Equal(HttpStatusCode.BadRequest, (await Update(manager, admin, "Driver")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Update(manager, admin, "Administrator", false)).StatusCode);
    }

    [Fact]
    public async Task Tenant_boundaries_are_enforced_for_reads_mutations_and_acceptance()
    {
        var outsider = await AddUser("Administrator", "other-subject", "other@example.test", "abcd-efgh-jkmn");
        using var other = await Client("other-subject", "abcd-efgh-jkmn");
        var invitation = await Invite(other);
        using var local = await Client();
        Assert.Empty((await local.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Invitations);
        Assert.Equal(HttpStatusCode.NotFound, (await Update(local, outsider, "Driver")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await local.PostAsync($"/api/users/invitations/{invitation.Id}/revoke", null)).StatusCode
        );
        Assert.Equal(HttpStatusCode.NotFound, (await local.GetAsync($"/api/users/{outsider.Id}/history")).StatusCode);
        // A local identity cannot accept an invitation for a different identity.
        Identity.Email = "dispatcher@example.test";
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await local.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).StatusCode
        );
        using var forgedTenant = await Client("dispatcher-test", "abcd-efgh-jkmn");
        Assert.Equal(HttpStatusCode.Forbidden, (await forgedTenant.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task Password_reset_reports_delivery_failures_and_retains_successful_requests()
    {
        var driver = await AddUser("Driver", "driver-subject", "driver2@example.test");
        using var manager = await Client();
        Identity.FailEmail = true;
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await manager.PostAsync($"/api/users/{driver.Id}/password-reset", null)).StatusCode
        );
        Identity.FailEmail = false;
        (await manager.PostAsync($"/api/users/{driver.Id}/password-reset", null)).EnsureSuccessStatusCode();
        Assert.Single((await manager.GetFromJsonAsync<HistoryModel[]>($"/api/users/{driver.Id}/history", Json))!);
    }

    [Fact]
    public async Task Anonymous_requests_and_missing_antiforgery_are_rejected()
    {
        using var anonymous = Application.CreateClient();
        anonymous.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/invitation")).StatusCode);
        using var manager = await Client();
        manager.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var response = await manager.PostAsJsonAsync(
            "/api/users/invitations",
            new
            {
                email = "driver@example.test",
                firstName = "Synthetic",
                lastName = "Driver",
                roles = new[] { "Driver" },
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Administrator", "Driver")]
    [InlineData("Dispatcher", "Driver")]
    [InlineData("Administrator", "Dispatcher")]
    public async Task Two_role_invitation_creates_one_membership_and_grants_both_roles(string first, string second)
    {
        await AddUser("Administrator", "admin-subject", "admin@example.test");
        using var manager = await Client("admin-subject");
        var roles = new[] { first, second };
        var invitation = await Invite(manager, roles);
        Assert.Equal(roles, invitation.Roles);
        using var recipient = await Client("invited-subject");
        Assert.Equal(
            roles,
            Assert.Single((await recipient.GetFromJsonAsync<PendingInvitationModel[]>("/api/invitation", Json))!).Roles
        );
        (await recipient.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).EnsureSuccessStatusCode();
        (await recipient.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).EnsureSuccessStatusCode();
        var session = await recipient.GetFromJsonAsync<JsonElement>("/api/auth/session");
        var tenant = Assert.Single(session.GetProperty("availableTenants").EnumerateArray());
        Assert.Equal(roles.Order(), tenant.GetProperty("roles").EnumerateArray().Select(x => x.GetString()).Order());
        Assert.Equal(TenantId, tenant.GetProperty("id").GetString());
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await recipient.PostAsJsonAsync("/api/auth/tenant-context", new { tenantId = TenantId })).StatusCode
        );
        Assert.Equal(
            roles.Contains("Administrator") ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
            (await recipient.GetAsync("/api/users")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await recipient.PostAsJsonAsync("/api/auth/tenant-context", new { tenantId = "abcd-efgh-jkmn" })
            ).StatusCode
        );
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(x => x.KeycloakUserId == "invited-subject");
        Assert.Equal(roles, (await db.TenantMemberships.SingleAsync(x => x.UserId == user.Id)).Roles);
        Assert.Single((await db.TenantMemberships.SingleAsync(x => x.UserId == user.Id)).History);
    }

    [Theory]
    [InlineData("Administrator")]
    [InlineData("Dispatcher")]
    public async Task Dispatcher_cannot_manage_Drivers_with_a_privileged_second_role(string privilegedRole)
    {
        await AddUser("Administrator", "admin-subject", "admin@example.test");
        var roles = new[] { "Driver", privilegedRole };
        var target = await AddUser(roles, "target-subject", "target@example.test");
        using var manager = await Client("dispatcher-test");
        using var admin = await Client("admin-subject");
        var invitation = await Invite(admin, roles);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Update(manager, target, "Driver")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.PostAsync($"/api/users/{target.Id}/password-reset", null)).StatusCode
        );
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"/api/users/{target.Id}/history")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.GetAsync($"/api/users/invitations/{invitation.Id}/history")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.PostAsync($"/api/users/invitations/{invitation.Id}/resend", null)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.PostAsync($"/api/users/invitations/{invitation.Id}/revoke", null)).StatusCode
        );
        var driver = await AddUser("Driver", "driver-subject", "driver2@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await Update(manager, driver, roles)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await manager.PostAsJsonAsync(
                    "/api/users/invitations",
                    new
                    {
                        email = "another@example.test",
                        firstName = "Synthetic",
                        lastName = "Invitee",
                        roles,
                    }
                )
            ).StatusCode
        );
    }

    [Fact]
    public async Task Role_changes_take_effect_immediately_preserve_history_and_ignore_order()
    {
        var admin = await AddUser(new[] { "Administrator", "Driver" }, "admin-subject", "admin@example.test");
        var driver = await AddUser("Driver", "driver-subject", "driver2@example.test");
        using var manager = await Client("admin-subject");
        using var recipient = await Client("driver-subject");
        (await Update(manager, driver, new[] { "Driver", "Administrator" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await recipient.GetAsync("/api/users")).StatusCode);
        var updated = (await manager.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Id == driver.Id
        );
        (await Update(manager, updated, new[] { "Administrator", "Driver" })).EnsureSuccessStatusCode();
        var reordered = (await manager.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Id == driver.Id
        );
        Assert.Equal(updated.Version, reordered.Version);
        var history = (await manager.GetFromJsonAsync<HistoryModel[]>($"/api/users/{driver.Id}/history", Json))!;
        Assert.Single(history);
        Assert.Equal("Roles changed", history[0].Action);
        Assert.Contains("Driver, Administrator", history[0].Details);
        Assert.Equal(HttpStatusCode.Conflict, (await Update(manager, driver, "Driver")).StatusCode);
        (await Update(manager, updated, "Driver")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await recipient.GetAsync("/api/users")).StatusCode);
        Assert.Single(
            (await recipient.GetFromJsonAsync<JsonElement>("/api/auth/session"))
                .GetProperty("availableTenants")
                .EnumerateArray()
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await Update(manager, admin, new[] { "Dispatcher", "Driver" })).StatusCode
        );
        (await Update(manager, admin, new[] { "Administrator", "Dispatcher" })).EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("[\"Driver\",\"Driver\"]")]
    [InlineData("[\"Driver\",\"Dispatcher\",\"Administrator\"]")]
    [InlineData("[\"Unknown\"]")]
    [InlineData("[\"Driver\",null]")]
    public async Task Invalid_role_sets_are_rejected_for_invites_and_edits(string rolesJson)
    {
        var admin = await AddUser("Administrator", "admin-subject", "admin@example.test");
        using var manager = await Client("admin-subject");
        var roles = JsonSerializer.Deserialize<string[]>(rolesJson);
        var payload = new
        {
            email = "driver@example.test",
            firstName = "Synthetic",
            lastName = "User",
            roles,
            isActive = true,
            displayName = admin.DisplayName,
            version = admin.Version,
        };
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await manager.PostAsJsonAsync("/api/users/invitations", payload)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await manager.PutAsJsonAsync($"/api/users/{admin.Id}", payload)).StatusCode
        );
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Invitations.ToListAsync());
        Assert.Equal(
            new[] { "Administrator" },
            (await db.TenantMemberships.SingleAsync(x => x.UserId == admin.Id)).Roles
        );
    }

    private async Task<HttpClient> Client(string? subject = null, string tenantId = TenantId)
    {
        if (subject is null)
        {
            subject = "dispatcher-test";
            await using var scope = Application.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(x => x.KeycloakUserId == subject);
            (await db.TenantMemberships.SingleAsync(x => x.UserId == user.Id && x.TenantId == tenantId)).SetRoles([
                "Administrator",
            ]);
            await db.SaveChangesAsync();
        }
        var client = Application.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        client.DefaultRequestHeaders.Add("X-Test-Tenant", tenantId);
        await AddAntiforgeryToken(client);
        return client;
    }

    private static Task<InvitationModel> Invite(
        HttpClient client,
        string role = "Driver",
        string email = "driver@example.test"
    ) => Invite(client, [role], email);

    private static async Task<InvitationModel> Invite(
        HttpClient client,
        string[] roles,
        string email = "driver@example.test"
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/users/invitations",
            new
            {
                email,
                firstName = "Synthetic",
                lastName = "Invitee",
                roles,
            }
        );
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<InvitationModel>(Json))!;
    }

    [Fact]
    public async Task Same_identity_can_accept_invitations_to_two_Tenants_with_independent_access_and_history()
    {
        const string otherTenant = "abcd-efgh-jkmn";
        await AddUser("Administrator", "local-admin", "local-admin@example.test");
        await AddUser("Administrator", "other-admin", "other-admin@example.test", otherTenant);
        using var local = await Client("local-admin");
        using var other = await Client("other-admin", otherTenant);
        var first = await Invite(local, new[] { "Dispatcher", "Driver" });
        var second = await Invite(other, "Administrator");
        using var recipient = await Client("invited-subject");
        Assert.Equal(2, (await recipient.GetFromJsonAsync<PendingInvitationModel[]>("/api/invitation", Json))!.Length);
        (await recipient.PostAsync($"/api/invitation/{first.Id}/accept", null)).EnsureSuccessStatusCode();
        var pending = Assert.Single(
            (await recipient.GetFromJsonAsync<PendingInvitationModel[]>("/api/invitation", Json))!
        );
        Assert.Equal(second.Id, pending.Id);
        // Existing local access does not bypass membership in the invited Organization.
        Identity.IsMember = false;
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await recipient.PostAsync($"/api/invitation/{second.Id}/accept", null)).StatusCode
        );
        Identity.IsMember = true;
        (await recipient.PostAsync($"/api/invitation/{second.Id}/accept", null)).EnsureSuccessStatusCode();
        var target = (await local.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Email == "driver@example.test"
        );
        var otherTarget = (await other.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Id == target.Id
        );
        Assert.Equal(new[] { "Administrator" }, otherTarget.Roles);
        (
            await local.PutAsJsonAsync(
                $"/api/users/{target.Id}",
                new
                {
                    displayName = "Local Driver",
                    firstName = "Overposted",
                    lastName = "Profile",
                    email = "changed@example.test",
                    roles = target.Roles,
                    isActive = true,
                    version = target.Version,
                }
            )
        ).EnsureSuccessStatusCode();
        target = (await local.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Id == target.Id
        );
        Assert.Equal("Local Driver", target.DisplayName);
        Assert.Equal("Synthetic", target.FirstName);
        Assert.Equal("Invitee", target.LastName);
        Assert.Equal("driver@example.test", target.Email);
        var unchanged = (await other.GetFromJsonAsync<UserManagementModel>("/api/users", Json))!.Users.Single(x =>
            x.Id == target.Id
        );
        Assert.Equal(otherTarget.DisplayName, unchanged.DisplayName);
        Assert.Equal(otherTarget.Version, unchanged.Version);
        using var otherSession = await Client("invited-subject", otherTenant);
        Assert.Equal(HttpStatusCode.Forbidden, (await recipient.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await otherSession.GetAsync("/api/users")).StatusCode);
        (await Update(local, target, "Driver", false)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await recipient.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await otherSession.GetAsync("/api/users")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await recipient.PostAsJsonAsync("/api/auth/tenant-context", new { tenantId = TenantId })).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await recipient.PostAsJsonAsync("/api/auth/tenant-context", new { tenantId = otherTenant })).StatusCode
        );
        var sessionMemberships = (await recipient.GetFromJsonAsync<JsonElement>("/api/auth/session"))
            .GetProperty("availableTenants")
            .EnumerateArray()
            .ToArray();
        Assert.All(sessionMemberships, x => Assert.Equal(otherTenant, x.GetProperty("id").GetString()));
        Assert.Single(sessionMemberships);
        var otherHistory = (await other.GetFromJsonAsync<HistoryModel[]>($"/api/users/{target.Id}/history", Json))!;
        Assert.Single(otherHistory);
        Assert.Equal("Invitation accepted", otherHistory[0].Action);
        Assert.Contains(
            (await local.GetFromJsonAsync<HistoryModel[]>($"/api/users/{target.Id}/history", Json))!,
            x => x.Action == "Deactivated"
        );
        // A replay cannot reactivate the old membership or duplicate another one.
        (await recipient.PostAsync($"/api/invitation/{first.Id}/accept", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await recipient.GetAsync("/api/users")).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (
                await local.PostAsJsonAsync(
                    "/api/users/invitations",
                    new
                    {
                        email = "DRIVER@example.test",
                        firstName = "Synthetic",
                        lastName = "Driver",
                        roles = new[] { "Driver" },
                    }
                )
            ).StatusCode
        );
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.Users.Where(x => x.KeycloakUserId == "invited-subject").ToListAsync());
        var memberships = await db.TenantMemberships.Where(x => x.UserId == target.Id).ToListAsync();
        Assert.Equal(2, memberships.Count);
        Assert.Equal(otherTarget.Version, memberships.Single(x => x.TenantId == otherTenant).Version);
    }

    [Fact]
    public async Task An_existing_User_can_be_invited_to_another_Tenant_but_email_cannot_link_a_different_identity()
    {
        await AddUser("Driver", "existing-subject", "driver@example.test", "abcd-efgh-jkmn");
        using var manager = await Client();
        var invitation = await Invite(manager);
        using var wrongIdentity = await Client("different-subject");
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await wrongIdentity.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).StatusCode
        );
        using var existing = await Client("existing-subject");
        (await existing.PostAsync($"/api/invitation/{invitation.Id}/accept", null)).EnsureSuccessStatusCode();
        var session = await existing.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.Equal(2, session.GetProperty("availableTenants").GetArrayLength());
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(x => x.KeycloakUserId == "existing-subject");
        Assert.Equal(user.Id, session.GetProperty("userId").GetGuid());
        Assert.Equal(2, await db.TenantMemberships.CountAsync(x => x.UserId == user.Id));
    }

    private Task<UserModel> AddUser(string role, string subject, string email, string tenantId = TenantId) =>
        AddUser([role], subject, email, tenantId);

    private async Task<UserModel> AddUser(string[] roles, string subject, string email, string tenantId = TenantId)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await db.Tenants.AnyAsync(x => x.Id == tenantId))
        {
            db.Tenants.Add(TenantAggregate.Create(tenantId, "Another Synthetic Tenant", "another-organization"));
            await db.SaveChangesAsync();
            await scope
                .ServiceProvider.GetRequiredService<IDynamicTenantSource<string>>()
                .AddTenantAsync(tenantId, CancellationToken.None);
        }
        var user = UserAggregate.Create("Synthetic", "User", subject, email);
        db.Users.Add(user);
        db.TenantMemberships.Add(TenantMembership.Create(tenantId, user.Id, roles));
        await db.SaveChangesAsync();
        return new UserModel(
            user.Id,
            user.FirstName,
            user.LastName,
            email,
            $"{user.FirstName} {user.LastName}",
            roles,
            true,
            0
        );
    }

    private static Task<HttpResponseMessage> Update(
        HttpClient client,
        UserModel user,
        string role,
        bool active = true,
        string? displayName = null
    ) => Update(client, user, [role], active, displayName);

    private static Task<HttpResponseMessage> Update(
        HttpClient client,
        UserModel user,
        string[] roles,
        bool active = true,
        string? displayName = null
    ) =>
        client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new
            {
                displayName = displayName ?? user.DisplayName,
                roles,
                isActive = active,
                version = user.Version,
            }
        );
}
