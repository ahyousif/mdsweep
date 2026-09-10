using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Mdsweep.Api.IntegrationTests;

public sealed class UserManagementMigrationTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Role_upgrade_preserves_single_roles_and_pending_invitation()
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invitation = InvitationAggregate.Create(
            "mdsw-eep2-3456",
            "driver@example.test",
            "Synthetic",
            "Invitee",
            ["Driver"],
            new string('a', 64),
            NodaTime.SystemClock.Instance.GetCurrentInstant()
        );
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260906001412_UserManagementAndInvitations");
        Assert.Equal(
            "Driver",
            await db.Database.SqlQueryRaw<string>("SELECT \"Role\" AS \"Value\" FROM invitations").SingleAsync()
        );
        Assert.Equal(
            "Dispatcher",
            await db.Database.SqlQueryRaw<string>("SELECT role AS \"Value\" FROM tenant_memberships").SingleAsync()
        );
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var upgraded = await db.Invitations.SingleAsync();
        Assert.Equal(invitation.Id, upgraded.Id);
        Assert.Equal(new[] { "Driver" }, upgraded.Roles);
        Assert.Equal("Pending", upgraded.Status);
        Assert.Equal(new[] { "Dispatcher" }, (await db.TenantMemberships.SingleAsync()).Roles);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Downgrade_cannot_discard_a_second_role(bool invitation)
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (invitation)
            db.Invitations.Add(
                InvitationAggregate.Create(
                    "mdsw-eep2-3456",
                    "driver@example.test",
                    "Synthetic",
                    "Invitee",
                    ["Driver", "Dispatcher"],
                    new string('a', 64),
                    NodaTime.SystemClock.Instance.GetCurrentInstant()
                )
            );
        else
            (await db.TenantMemberships.SingleAsync()).SetRoles(["Dispatcher", "Driver"]);
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            db.GetService<IMigrator>().MigrateAsync("20260906001412_UserManagementAndInvitations")
        );
        Assert.Contains("Cannot downgrade", error.MessageText);
    }

    [Fact]
    public async Task Access_baseline_upgrade_preserves_User_email_and_memberships_in_multiple_Tenants()
    {
        // Start with a fresh database: main intentionally cannot downgrade removed preview data.
        var connection = new NpgsqlConnectionStringBuilder(DatabaseConnectionString) { Database = "baseline_upgrade" };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection.ConnectionString, postgres => postgres.UseNodaTime())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var userId = Guid.NewGuid();
        var membershipIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260906001412_UserManagementAndInvitations");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO tenants (id, name, keycloak_organization_id) VALUES
                ('mdsw-eep2-3456', 'Synthetic Tenant', 'synthetic-tenant'),
                ('abcd-efgh-jkmn', 'Another Synthetic Tenant', 'another-organization');
            INSERT INTO users (id, first_name, last_name, keycloak_user_id, email)
                VALUES ({userId}, 'Synthetic', 'User', 'synthetic-user', 'synthetic@example.test');
            INSERT INTO tenant_memberships (id, tenant_id, user_id, role) VALUES
                ({membershipIds[0]}, 'mdsw-eep2-3456', {userId}, 'Dispatcher'),
                ({membershipIds[1]}, 'abcd-efgh-jkmn', {userId}, 'Driver');
            """
        );
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var user = await db.Users.SingleAsync();
        Assert.Equal(userId, user.Id);
        Assert.Equal("synthetic@example.test", user.Email);
        var memberships = await db.TenantMemberships.ToListAsync();
        Assert.Equal(membershipIds.Order(), memberships.Select(x => x.Id).Order());
        Assert.All(memberships, x => Assert.True(x.IsActive));
        Assert.Equal(new[] { "Dispatcher" }, memberships.Single(x => x.TenantId == "mdsw-eep2-3456").Roles);
        Assert.Equal(new[] { "Driver" }, memberships.Single(x => x.TenantId == "abcd-efgh-jkmn").Roles);
        Assert.Equal(
            0,
            await db
                .Database.SqlQueryRaw<int>(
                    "SELECT count(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'users' AND column_name IN ('tenant_id', 'is_active', 'version')"
                )
                .SingleAsync()
        );
        Assert.Empty(await db.Invitations.ToListAsync());
        Assert.Equal(
            0,
            await db
                .Database.SqlQueryRaw<int>(
                    "SELECT count(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('user_access_history', 'invitation_history')"
                )
                .SingleAsync()
        );
    }

    [Fact]
    public async Task Role_upgrade_preserves_membership_access_version()
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await db.TenantMemberships.SingleAsync();
        membership.SetActive(false);
        await db.SaveChangesAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260906001412_UserManagementAndInvitations");
        Assert.False(
            await db.Database.SqlQueryRaw<bool>("SELECT is_active AS \"Value\" FROM tenant_memberships").SingleAsync()
        );
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var restored = await db.TenantMemberships.SingleAsync();
        Assert.Equal(membership.Id, restored.Id);
        Assert.False(restored.IsActive);
        Assert.Equal(1, restored.Version);
    }

    [Fact]
    public async Task Multiple_Tenants_are_allowed_but_duplicate_memberships_are_rejected()
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync();
        db.Tenants.Add(TenantAggregate.Create("abcd-efgh-jkmn", "Another Synthetic Tenant", "another-organization"));
        db.TenantMemberships.Add(TenantMembership.Create("abcd-efgh-jkmn", user.Id, "Driver"));
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.TenantMemberships.CountAsync());
        db.TenantMemberships.Add(TenantMembership.Create("abcd-efgh-jkmn", user.Id, "Administrator"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260906001412_UserManagementAndInvitations");
        Assert.Equal(
            2,
            await db
                .Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM tenant_memberships")
                .SingleAsync()
        );
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.TenantMemberships.CountAsync());
    }
}
