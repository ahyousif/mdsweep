using System.Data;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mdsweep.Utility.BootstrapTenant;

public sealed class BootstrapTenantService(ApplicationDbContext db)
{
    private const string AdministratorRole = "Administrator";

    public async Task<BootstrapTenantResult> ExecuteAsync(
        BootstrapTenantOptions options,
        CancellationToken cancellationToken = default
    )
    {
        var schemaResult = await CheckSchemaAsync(cancellationToken);
        if (schemaResult is not null)
        {
            return schemaResult;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken
        );

        try
        {
            var normalizedEmail = options.Email.Trim().ToLowerInvariant();
            var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == options.TenantId, cancellationToken);
            var subjectUser = await db.Users.SingleOrDefaultAsync(
                x => x.KeycloakUserId == options.KeycloakUserId,
                cancellationToken
            );
            var emailUsers = await db.Users
                .Where(x => x.Email.ToLower() == normalizedEmail)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (emailUsers.Count > 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict($"Email {normalizedEmail} matches more than one existing User.");
            }

            var emailUser = emailUsers.SingleOrDefault();

            var conflict = FindConflict(options, normalizedEmail, tenant, subjectUser, emailUser);
            if (conflict is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(conflict);
            }

            if (tenant is null && subjectUser is null && emailUser is null)
            {
                var newTenant = TenantAggregate.Create(options.TenantId, options.TenantName);
                var newUser = UserAggregate.Create(
                    options.FirstName,
                    options.LastName,
                    options.KeycloakUserId,
                    normalizedEmail
                );
                var newMembership = TenantMembership.Create(
                    newTenant.Id,
                    newUser.Id,
                    options.DisplayName,
                    [AdministratorRole]
                );

                db.Tenants.Add(newTenant);
                db.Users.Add(newUser);
                db.TenantMemberships.Add(newMembership);

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new BootstrapTenantResult(
                    BootstrapTenantStatus.Created,
                    $"Tenant created: {newTenant.Name}\n"
                        + $"User created: {newUser.Email}\n"
                        + "Administrator membership created.\n\n"
                        + "Bootstrap completed successfully."
                );
            }

            if (tenant is null || subjectUser is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict("Only part of the requested Tenant and User already exists.");
            }

            var membership = await db.TenantMemberships.SingleOrDefaultAsync(
                x => x.TenantId == tenant.Id && x.UserId == subjectUser.Id,
                cancellationToken
            );

            if (membership is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict("The Tenant and User exist, but their Tenant Membership is missing.");
            }

            if (!membership.IsActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict("The existing Tenant Membership is disabled.");
            }

            if (!membership.Roles.Contains(AdministratorRole, StringComparer.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict("The existing Tenant Membership does not have Administrator access.");
            }

            await transaction.RollbackAsync(cancellationToken);
            return new BootstrapTenantResult(
                BootstrapTenantStatus.AlreadySatisfied,
                "Tenant bootstrap is already satisfied.\nNo changes were made."
            );
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict("The database rejected the bootstrap because existing data or a constraint conflicts.");
        }
    }

    private async Task<BootstrapTenantResult?> CheckSchemaAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                return SchemaUnavailable("The MDSweep application database is unavailable.");
            }

            var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                return SchemaUnavailable("The MDSweep application database has pending EF migrations.");
            }

            // Probe each required bootstrap table even when migration history reports current.
            _ = await db.Tenants.AsNoTracking().AnyAsync(cancellationToken);
            _ = await db.Users.AsNoTracking().AnyAsync(cancellationToken);
            _ = await db.TenantMemberships.AsNoTracking().AnyAsync(cancellationToken);

            return null;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            return SchemaUnavailable("The expected MDSweep application schema is not usable.");
        }
    }

    private static string? FindConflict(
        BootstrapTenantOptions options,
        string normalizedEmail,
        TenantAggregate? tenant,
        UserAggregate? subjectUser,
        UserAggregate? emailUser
    )
    {
        if (tenant is not null && !string.Equals(tenant.Name, options.TenantName, StringComparison.Ordinal))
        {
            return $"Tenant {options.TenantId} already exists with name '{tenant.Name}'.";
        }

        if (subjectUser is not null && !string.Equals(subjectUser.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return $"User with Keycloak subject {options.KeycloakUserId} already exists with email {subjectUser.Email}.";
        }

        if (emailUser is not null && (subjectUser is null || emailUser.Id != subjectUser.Id))
        {
            return $"Email {normalizedEmail} already belongs to a different User.";
        }

        return null;
    }

    private static BootstrapTenantResult Conflict(string message) =>
        new(BootstrapTenantStatus.Conflict, $"Bootstrap aborted.\n\n{message}\nNo changes were made.");

    private static BootstrapTenantResult SchemaUnavailable(string message) =>
        new(
            BootstrapTenantStatus.SchemaUnavailable,
            $"Bootstrap aborted.\n\n{message}\nDeploy and migrate the API before running this job.\nNo changes were made."
        );
}
