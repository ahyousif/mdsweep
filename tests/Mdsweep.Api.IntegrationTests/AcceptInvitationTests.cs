using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mdsweep.Application.Common.Security;
using Mdsweep.Infrastructure.Persistence;
using NodaTime;
using NodaSystemClock = NodaTime.SystemClock;

namespace Mdsweep.Api.IntegrationTests;

public sealed class AcceptInvitationTests : MdsweepIntegrationTest
{
    private const string TenantId = "mdsw-eep2-3456";

    [Fact]
    public async Task Matching_email_accepts_without_email_verification_and_creates_the_membership()
    {
        const string token = "matching-token";
        const string subject = "matching-invitee";
        const string email = "matching-invitee@example.test";
        await AddInvitation(token, email, NodaSystemClock.Instance.GetCurrentInstant() + Duration.FromHours(1));
        using var client = CreateInviteeClient(subject, email);
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/users/invitations/accept",
            new { token }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertAcceptedMembershipExists(email, subject);
    }

    [Fact]
    public async Task Matching_existing_user_receives_the_membership_before_acceptance_returns()
    {
        const string token = "existing-user-token";
        const string subject = "existing-invitee";
        const string email = "existing-invitee@example.test";

        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            setupDb.Users.Add(UserAggregate.Create("Existing", "Invitee", subject, email));
            await setupDb.SaveChangesAsync();
        }

        await AddInvitation(token, email, NodaSystemClock.Instance.GetCurrentInstant() + Duration.FromHours(1));
        using var client = CreateInviteeClient(subject, email);
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/users/invitations/accept",
            new { token }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertAcceptedMembershipExists(email, subject);

        await using var verificationScope = Application.Services.CreateAsyncScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.Users.Where(x => x.KeycloakUserId == subject).ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Acceptance_rejects_an_existing_tenant_membership_without_consuming_the_invitation(
        bool isActive
    )
    {
        const string token = "existing-membership-token";
        const string subject = "existing-member";
        const string email = "existing-member@example.test";

        await using (var setupScope = Application.Services.CreateAsyncScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = UserAggregate.Create("Existing", "Member", subject, email);
            var membership = TenantMembership.Create(
                TenantId,
                user.Id,
                "Existing Member",
                ["Driver"]
            );
            membership.SetActive(isActive);
            setupDb.Users.Add(user);
            setupDb.TenantMemberships.Add(membership);
            await setupDb.SaveChangesAsync();
        }

        await AddInvitation(token, email, NodaSystemClock.Instance.GetCurrentInstant() + Duration.FromHours(1));
        using var client = CreateInviteeClient(subject, email);
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/users/invitations/accept",
            new { token }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var verificationScope = Application.Services.CreateAsyncScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            InvitationStatus.Pending,
            (await db.Invitations.SingleAsync(x => x.Email == email)).Status
        );
        var persistedMembership = await db.TenantMemberships.SingleAsync(x =>
            x.TenantId == TenantId && x.UserId == db.Users.Single(user => user.KeycloakUserId == subject).Id
        );
        Assert.Equal(isActive, persistedMembership.IsActive);
        Assert.Equal(["Driver"], persistedMembership.Roles);
    }

    [Fact]
    public async Task Wrong_email_is_rejected_before_invitation_or_membership_mutation()
    {
        const string token = "mismatch-token";
        const string invitedEmail = "invited-user@example.test";
        const string subject = "existing-user";
        await AddInvitation(token, invitedEmail, NodaSystemClock.Instance.GetCurrentInstant() + Duration.FromHours(1));
        using var client = CreateInviteeClient(subject, "existing-user@example.test");
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/users/invitations/accept",
            new { token }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("invitationEmailMismatch", out _));
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            InvitationStatus.Pending,
            (await db.Invitations.SingleAsync(x => x.Email == invitedEmail)).Status
        );
        Assert.DoesNotContain(await db.Users.ToListAsync(), x => x.KeycloakUserId == subject);
        Assert.Single(await db.TenantMemberships.ToListAsync());
    }

    [Theory]
    [InlineData("unknown-token")]
    [InlineData("expired-token")]
    public async Task Invalid_or_expired_token_remains_rejected(string token)
    {
        if (token == "expired-token")
        {
            await AddInvitation(
                token,
                "expired-invitee@example.test",
                NodaSystemClock.Instance.GetCurrentInstant() - Duration.FromMinutes(1)
            );
        }
        using var client = CreateInviteeClient("invitee", "expired-invitee@example.test");
        await AddAntiforgeryToken(client);

        using var response = await client.PostAsJsonAsync(
            "/api/users/invitations/accept",
            new { token }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateInviteeClient(string subject, string email)
    {
        var client = Application.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        client.DefaultRequestHeaders.Add("X-Test-Email-Verified", bool.FalseString);
        return client;
    }

    private async Task AddInvitation(string token, string email, Instant expiresAt)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var tokenService = services.GetRequiredService<ITokenService>();
        var invitation = InvitationAggregate.Create(
            TenantId,
            email,
            "Synthetic",
            "Invitee",
            ["Driver"],
            token,
            tokenService.Hash(token),
            expiresAt
        );
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();
    }

    private async Task AssertAcceptedMembershipExists(string email, string subject)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invitation = await db.Invitations.SingleAsync(x => x.Email == email);
        var user = await db.Users.SingleOrDefaultAsync(x => x.KeycloakUserId == subject);

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.NotNull(user);
        Assert.True(await db.TenantMemberships.AnyAsync(x => x.TenantId == TenantId && x.UserId == user.Id));
    }
}
