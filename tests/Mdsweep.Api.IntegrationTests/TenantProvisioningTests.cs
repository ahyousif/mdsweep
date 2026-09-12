using System.Reflection;
using JasperFx.MultiTenancy;
using Mdsweep.Application.Common.Configuration;
using Mdsweep.Application.Common.Email;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Users.Invitations.DomainEventHandlers;
using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Utility.Commands;
using Mdsweep.Utility.TenantProvisioning;
using NodaTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TenantProvisioningTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();
    private readonly RecordingEmailSender emailSender = new();
    private readonly IncrementingTokenService tokenService = new();
    private readonly DynamicTenantSourceProxy tenantSourceProxy;
    private readonly IDynamicTenantSource<string> tenantSource;

    private static readonly TenantProvisioningOptions Options = new(
        "mdsw-eep2-3456",
        "MDSweep",
        "Admin@Example.Test",
        "Synthetic",
        "Administrator",
        "Operations Owner"
    );

    public TenantProvisioningTests()
    {
        tenantSource = DispatchProxy.Create<IDynamicTenantSource<string>, DynamicTenantSourceProxy>();
        tenantSourceProxy = (DynamicTenantSourceProxy)(object)tenantSource;
    }

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => database.DisposeAsync().AsTask();

    [Fact]
    public async Task Available_database_and_new_tenant_creates_administrator_invitation_and_sends_email()
    {
        var result = await ExecuteAsync();

        Assert.Equal(TenantProvisioningStatus.Provisioned, result.Status);
        await using var db = CreateDbContext();
        Assert.Equal(Options.TenantName, (await db.Tenants.SingleAsync()).Name);
        var invitation = await db.Invitations.SingleAsync();
        Assert.Equal("admin@example.test", invitation.Email);
        Assert.Equal(Options.AdminDisplayName, invitation.DisplayName);
        Assert.Contains("Administrator", invitation.Roles);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Single(emailSender.Messages);
        Assert.Equal("admin@example.test", emailSender.Messages[0].To);
        Assert.Contains(Options.TenantId, tenantSourceProxy.RegisteredTenantIds);
    }

    [Fact]
    public async Task Compatible_rerun_reissues_the_pending_administrator_invitation()
    {
        Assert.Equal(TenantProvisioningStatus.Provisioned, (await ExecuteAsync()).Status);
        string originalTokenHash;
        await using (var original = CreateDbContext())
        {
            originalTokenHash = (await original.Invitations.SingleAsync()).TokenHash;
        }

        var result = await ExecuteAsync();

        Assert.Equal(TenantProvisioningStatus.Provisioned, result.Status);
        await using var db = CreateDbContext();
        Assert.Equal(1, await db.Tenants.CountAsync());
        Assert.Equal(1, await db.Invitations.CountAsync());
        Assert.NotEqual(originalTokenHash, (await db.Invitations.SingleAsync()).TokenHash);
        Assert.Equal(2, emailSender.Messages.Count);
    }

    [Fact]
    public async Task Rerun_reissues_pending_administrator_invitation_after_email_failure()
    {
        emailSender.FailNext = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteAsync());
        Assert.Single(tenantSourceProxy.RegisteredTenantIds);
        Assert.Equal(Options.TenantId, tenantSourceProxy.RegisteredTenantIds[0]);

        await using (var committed = CreateDbContext())
        {
            Assert.Single(await committed.Tenants.ToListAsync());
            Assert.Single(await committed.Invitations.ToListAsync());
        }

        var result = await ExecuteAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(2, emailSender.Attempts);
        Assert.Single(emailSender.Messages);
        Assert.Equal(2, tenantSourceProxy.RegisteredTenantIds.Count);
    }

    [Fact]
    public async Task Conflicting_tenant_name_fails_without_mutation()
    {
        await using (var db = CreateDbContext())
        {
            db.Tenants.Add(TenantAggregate.Create(Options.TenantId, "Different Tenant"));
            await db.SaveChangesAsync();
        }

        var result = await ExecuteAsync();

        Assert.Equal(TenantProvisioningStatus.Conflict, result.Status);
        Assert.Contains("Different Tenant", result.Message);
        await using var verification = CreateDbContext();
        Assert.Empty(await verification.Invitations.ToListAsync());
    }

    [Fact]
    public async Task Provisioning_does_not_create_a_local_user()
    {
        await ExecuteAsync();

        await using var db = CreateDbContext();
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task Provisioning_does_not_create_a_tenant_membership()
    {
        await ExecuteAsync();

        await using var db = CreateDbContext();
        Assert.Empty(await db.TenantMemberships.ToListAsync());
    }

    [Fact]
    public async Task Persistence_failure_rolls_back_tenant_and_invitation()
    {
        var invalidOptions = Options with { AdminEmail = $"admin@{new string('x', 300)}.test" };

        var result = await ExecuteAsync(invalidOptions);

        Assert.Equal(TenantProvisioningStatus.Conflict, result.Status);
        await using var db = CreateDbContext();
        Assert.Empty(await db.Tenants.ToListAsync());
        Assert.Empty(await db.Invitations.ToListAsync());
        Assert.Empty(emailSender.Messages);
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .ConfigureForMdsweep(database.GetConnectionString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<TenantProvisioningResult> ExecuteAsync(TenantProvisioningOptions? options = null)
    {
        await using var db = CreateDbContext();
        var emailHandler = new SendEmailWhenInvitationCreatedHandler(
            emailSender,
            db,
            Microsoft.Extensions.Options.Options.Create(new WebOptions { BaseUrl = "https://web.mdsweep.test" })
        );
        var service = new TenantProvisioningService(
            db,
            tokenService,
            NodaTime.SystemClock.Instance,
            tenantSource,
            emailHandler
        );
        return await service.ExecuteAsync(options ?? Options);
    }

    private sealed class IncrementingTokenService : ITokenService
    {
        private int sequence;

        public Token Generate(int lengthBytes = 32)
        {
            var value = $"synthetic-invitation-token-{Interlocked.Increment(ref sequence)}";
            return new Token(value, Hash(value));
        }

        public string Hash(string token) => $"hash-{token}";
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public int Attempts { get; private set; }
        public bool FailNext { get; set; }
        public List<EmailMessage> Messages { get; } = [];

        public Task SendEmailAsync(
            string to,
            string subject,
            string body,
            string? from = null,
            bool isHtml = false,
            CancellationToken ct = default
        )
        {
            Attempts++;
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("Synthetic email failure.");
            }

            Messages.Add(new EmailMessage(to, subject));
            return Task.CompletedTask;
        }
    }

    private sealed record EmailMessage(string To, string Subject);

    private class DynamicTenantSourceProxy : DispatchProxy
    {
        public List<string> RegisteredTenantIds { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IDynamicTenantSource<string>.AddTenantAsync) && args is not null)
            {
                RegisteredTenantIds.Add((string)args[0]!);
                return CompletedResult(targetMethod.ReturnType, args[0]);
            }

            throw new NotSupportedException(targetMethod?.Name);
        }

        private static object CompletedResult(Type returnType, object? result)
        {
            if (returnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            if (returnType == typeof(ValueTask))
            {
                return ValueTask.CompletedTask;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(returnType.GenericTypeArguments[0])
                    .Invoke(null, [result])!;
            }

            return Activator.CreateInstance(returnType, result)!;
        }
    }
}
