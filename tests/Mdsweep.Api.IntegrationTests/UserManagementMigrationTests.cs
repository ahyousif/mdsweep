using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Mdsweep.Api.IntegrationTests;

public sealed class UserManagementMigrationTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Role_upgrade_preserves_single_roles_and_pending_invitation_history()
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var invitation = InvitationAggregate.Create("mdsw-eep2-3456", "driver@example.test", "Synthetic", "Invitee",
            ["Driver"], "dispatcher-test", NodaTime.SystemClock.Instance.GetCurrentInstant());
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260906001412_UserManagementAndInvitations");
        Assert.Equal("Driver", await db.Database.SqlQueryRaw<string>("SELECT \"Role\" AS \"Value\" FROM invitations").SingleAsync());
        Assert.Equal("Dispatcher", await db.Database.SqlQueryRaw<string>("SELECT role AS \"Value\" FROM tenant_memberships").SingleAsync());
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var upgraded = await db.Invitations.SingleAsync();
        Assert.Equal(invitation.Id, upgraded.Id);
        Assert.Equal(new[] { "Driver" }, upgraded.Roles);
        Assert.Equal("Pending", upgraded.Status);
        Assert.Single(upgraded.History);
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
            db.Invitations.Add(InvitationAggregate.Create("mdsw-eep2-3456", "driver@example.test", "Synthetic", "Invitee",
                ["Driver", "Dispatcher"], "dispatcher-test", NodaTime.SystemClock.Instance.GetCurrentInstant()));
        else
            (await db.TenantMemberships.SingleAsync()).SetRoles(["Dispatcher", "Driver"]);
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync("20260906001412_UserManagementAndInvitations"));
        Assert.Contains("Cannot downgrade", error.MessageText);
    }

    [Fact]
    public async Task Baseline_upgrade_preserves_existing_User_and_membership()
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = await db.Users.Select(x => x.Id).SingleAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260831065755_InitialSchema");
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var user = await db.Users.SingleAsync();
        Assert.Equal(userId, user.Id);
        Assert.True(user.IsActive);
        Assert.Equal("mdsw-eep2-3456", user.TenantId);
        Assert.Equal(new[] { "Dispatcher" }, (await db.TenantMemberships.SingleAsync()).Roles);
        Assert.Empty(await db.Invitations.ToListAsync());
    }

    [Fact]
    public async Task Upgrade_rejects_multiple_memberships_instead_of_selecting_a_role()
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = await db.Users.Select(x => x.Id).SingleAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260831065755_InitialSchema");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO tenant_memberships (id, tenant_id, user_id, role) VALUES ({Guid.CreateVersion7()}, 'mdsw-eep2-3456', {userId}, 'Driver')");
        var error = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync());
        Assert.Contains("exactly one Tenant Membership", error.MessageText);
    }
}
