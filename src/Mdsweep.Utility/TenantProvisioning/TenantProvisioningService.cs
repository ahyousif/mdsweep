using System.Data;
using JasperFx.MultiTenancy;
using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Users.Invitations.DomainEventHandlers;
using Mdsweep.Application.Users.Invitations.Invite;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users;
using Mdsweep.Domain.Users.Events;
using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Utility.Commands;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Mdsweep.Utility.TenantProvisioning;

public sealed class TenantProvisioningService(
    ApplicationDbContext db,
    ITokenService tokenService,
    IClock clock,
    SendEmailWhenInvitationCreatedHandler emailHandler
)
{
    private const string AdministratorRole = "Administrator";

    public async Task<TenantProvisioningResult> ExecuteAsync(
        TenantProvisioningOptions options,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken
        );

        try
        {
            var email = options.AdminEmail.Trim().ToLowerInvariant();
            var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == options.TenantId, cancellationToken);
            if (tenant is not null && !string.Equals(tenant.Name, options.TenantName, StringComparison.Ordinal))
            {
                return await ConflictAsync(
                    transaction,
                    $"Tenant {options.TenantId} already exists with name '{tenant.Name}'.",
                    cancellationToken
                );
            }

            if (tenant is not null)
            {
                var compatibleInvitation = await db.Invitations.AnyAsync(
                    invitation =>
                        invitation.TenantId == tenant.Id
                        && invitation.Email == email
                        && invitation.Status == InvitationStatus.Pending
                        && invitation.ExpiresAt > clock.GetCurrentInstant()
                        && invitation.Roles.Contains(AdministratorRole),
                    cancellationToken
                );
                if (compatibleInvitation)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new TenantProvisioningResult(
                        TenantProvisioningStatus.AlreadySatisfied,
                        "Tenant provisioning is already satisfied. No changes were made."
                    );
                }

                var user = await db.Users.SingleOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);
                if (user is not null)
                {
                    var membership = await db.TenantMemberships.SingleOrDefaultAsync(
                        x => x.TenantId == tenant.Id && x.UserId == user.Id,
                        cancellationToken
                    );
                    if (membership is not null && membership.IsActive && membership.Roles.Contains(AdministratorRole))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return new TenantProvisioningResult(
                            TenantProvisioningStatus.AlreadySatisfied,
                            "Tenant provisioning is already satisfied. No changes were made."
                        );
                    }

                    if (membership is not null)
                    {
                        return await ConflictAsync(
                            transaction,
                            "The administrator already has incompatible Tenant access.",
                            cancellationToken
                        );
                    }
                }
            }

            tenant ??= TenantAggregate.Create(options.TenantId, options.TenantName);
            if (db.Entry(tenant).State == EntityState.Detached)
            {
                db.Tenants.Add(tenant);
            }

            var inviteHandler = new InviteUserHandler(db, tokenService, clock);
            var (result, messages) = await inviteHandler.Handle(
                new InviteUserCommand(
                    email,
                    options.AdminFirstName,
                    options.AdminLastName,
                    [AdministratorRole],
                    options.AdminDisplayName
                ),
                new TenantId(options.TenantId),
                cancellationToken
            );
            if (!result.IsSuccess)
            {
                return await ConflictAsync(
                    transaction,
                    string.Join(" ", result.ValidationErrors.Select(error => error.ErrorMessage)),
                    cancellationToken
                );
            }

            var invitationEvent = messages.OfType<InvitationCreatedDomainEvent>().Single();
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await emailHandler.Handle(invitationEvent, new TenantId(options.TenantId), cancellationToken);

            return new TenantProvisioningResult(
                TenantProvisioningStatus.Provisioned,
                $"Tenant provisioned: {tenant.Name}\nAdministrator invitation sent to {email}."
            );
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new TenantProvisioningResult(
                TenantProvisioningStatus.Conflict,
                "Provisioning failed because the database rejected conflicting or invalid data. No partial application state was retained."
            );
        }
    }

    private static async Task<TenantProvisioningResult> ConflictAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        string message,
        CancellationToken cancellationToken
    )
    {
        await transaction.RollbackAsync(cancellationToken);
        return new TenantProvisioningResult(TenantProvisioningStatus.Conflict, $"Provisioning aborted. {message}");
    }
}
