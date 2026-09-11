using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Utility.BootstrapTenant;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mdsweep.Api.IntegrationTests;

public sealed class BootstrapTenantTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();

    private static readonly BootstrapTenantOptions Options =
        new(
            "mdsw-eep2-3456",
            "MDSweep",
            "synthetic-keycloak-sub",
            "Admin@Example.Test",
            "Synthetic",
            "Administrator",
            "Synthetic Administrator"
        );

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => database.DisposeAsync().AsTask();

    [Fact]
    public async Task Empty_database_creates_tenant_user_and_administrator_membership()
    {
        await ResetAsync();

        var result = await ExecuteAsync();

        Assert.Equal(BootstrapTenantStatus.Created, result.Status);
        await using var db = CreateDbContext();
        var tenant = await db.Tenants.SingleAsync();
        var user = await db.Users.SingleAsync();
        var membership = await db.TenantMemberships.SingleAsync();
        Assert.Equal(Options.TenantName, tenant.Name);
        Assert.Equal("admin@example.test", user.Email);
        Assert.Equal(Options.KeycloakUserId, user.KeycloakUserId);
        Assert.Equal(tenant.Id, membership.TenantId);
        Assert.Equal(user.Id, membership.UserId);
        Assert.True(membership.IsActive);
        Assert.Contains("Administrator", membership.Roles);
    }

    [Fact]
    public async Task Exact_rerun_is_idempotent()
    {
        await ResetAsync();
        Assert.Equal(BootstrapTenantStatus.Created, (await ExecuteAsync()).Status);

        var result = await ExecuteAsync();

        Assert.Equal(BootstrapTenantStatus.AlreadySatisfied, result.Status);
        await AssertCountsAsync(1, 1, 1);
    }

    [Fact]
    public async Task Conflicting_tenant_name_fails_without_mutation()
    {
        await ResetAsync();
        await using (var db = CreateDbContext())
        {
            db.Tenants.Add(TenantAggregate.Create(Options.TenantId, "Different Tenant"));
            await db.SaveChangesAsync();
        }

        var result = await ExecuteAsync();

        Assert.Equal(BootstrapTenantStatus.Conflict, result.Status);
        Assert.Contains("Different Tenant", result.Message);
        await AssertCountsAsync(1, 0, 0);
    }

    [Fact]
    public async Task Conflicting_keycloak_subject_email_mapping_fails()
    {
        await ResetAsync();
        await using (var db = CreateDbContext())
        {
            db.Users.Add(
                UserAggregate.Create("Other", "User", Options.KeycloakUserId, "other@example.test")
            );
            await db.SaveChangesAsync();
        }

        var result = await ExecuteAsync();

        Assert.Equal(BootstrapTenantStatus.Conflict, result.Status);
        Assert.Contains("other@example.test", result.Message);
        await AssertCountsAsync(0, 1, 0);
    }

    [Fact]
    public async Task Inactive_membership_fails_without_reactivation()
    {
        await ResetAsync();
        var membershipId = await SeedCompleteStateAsync(["Administrator"], active: false);

        var result = await ExecuteAsync();

        Assert.Equal(BootstrapTenantStatus.Conflict, result.Status);
        await using var db = CreateDbContext();
        Assert.False((await db.TenantMemberships.FindAsync(membershipId))!.IsActive);
    }

    [Fact]
    public async Task Non_administrator_membership_fails_without_elevation()
    {
        await ResetAsync();
        var membershipId = await SeedCompleteStateAsync(["Dispatcher"]);

        var result = await ExecuteAsync();

        Assert.Equal(BootstrapTenantStatus.Conflict, result.Status);
        await using var db = CreateDbContext();
        Assert.Equal(["Dispatcher"], (await db.TenantMemberships.FindAsync(membershipId))!.Roles);
    }

    [Fact]
    public async Task Persistence_failure_rolls_back_all_inserts()
    {
        await ResetAsync();
        var invalidOptions = Options with { Email = $"admin@{new string('x', 300)}.test" };

        var result = await ExecuteAsync(invalidOptions);

        Assert.Equal(BootstrapTenantStatus.Conflict, result.Status);
        await AssertCountsAsync(0, 0, 0);
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .ConfigureForMdsweep(database.GetConnectionString())
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<BootstrapTenantResult> ExecuteAsync(BootstrapTenantOptions? options = null)
    {
        await using var db = CreateDbContext();
        return await new BootstrapTenantService(db).ExecuteAsync(options ?? Options);
    }

    private async Task ResetAsync()
    {
        await using var db = CreateDbContext();
        await db.TenantMemberships.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
        await db.Tenants.ExecuteDeleteAsync();
    }

    private async Task<Guid> SeedCompleteStateAsync(string[] roles, bool active = true)
    {
        await using var db = CreateDbContext();
        var tenant = TenantAggregate.Create(Options.TenantId, Options.TenantName);
        var user = UserAggregate.Create(
            Options.FirstName,
            Options.LastName,
            Options.KeycloakUserId,
            Options.Email.Trim().ToLowerInvariant()
        );
        var membership = TenantMembership.Create(tenant.Id, user.Id, Options.DisplayName, roles);
        membership.SetActive(active);
        db.AddRange(tenant, user, membership);
        await db.SaveChangesAsync();
        return membership.Id;
    }

    private async Task AssertCountsAsync(int tenants, int users, int memberships)
    {
        await using var db = CreateDbContext();
        Assert.Equal(tenants, await db.Tenants.CountAsync());
        Assert.Equal(users, await db.Users.CountAsync());
        Assert.Equal(memberships, await db.TenantMemberships.CountAsync());
    }
}
