using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Email;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users.Events;

namespace Mdsweep.Application.Users.DomainEventHandlers;

public sealed class SendInvitationEmailHandler(IEmailSender emailSender, IRepository repository)
{
    public async Task Handle(InvitationCreatedDomainEvent @event, TenantId tenantId, CancellationToken ct)
    {
        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(tenantId.Value, ct);

        Guard.Against.Null(tenant);

        var subject = $"You've been invited to join {tenant.Name}";
        var expiresAt = @event.ExpiresAt.ToDateTimeOffset().ToString("u");
        var body = $"""
            Hi {@event.FirstName},

            You've been invited to join {tenant.Name}.

            Invitation token: {@event.Token}
            Expires at: {expiresAt}
            """;

        await emailSender.SendEmailAsync(@event.Email, subject, body, tenant.DefaultSenderEmail, ct);
    }
}
