using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Configuration;
using Mdsweep.Application.Common.Email;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users.Events;
using Microsoft.Extensions.Options;

namespace Mdsweep.Application.Users.DomainEventHandlers;

public sealed class SendInvitationEmailHandler(
    IEmailSender emailSender,
    IRepository repository,
    IOptions<WebOptions> webOptions
)
{
    public async Task Handle(InvitationCreatedDomainEvent @event, TenantId tenantId, CancellationToken ct)
    {
        var tenant = await repository.GetByIdAsync<TenantAggregate, string>(tenantId.Value, ct);

        Guard.Against.Null(tenant);

        var baseUrl = webOptions.Value.BaseUrl.TrimEnd('/');
        var invitationUrl = $"{baseUrl}/invitations/accept" + $"?token={Uri.EscapeDataString(@event.Token)}";

        var subject = $"You've been invited to join {tenant.Name}";

        var expiresAt = @event.ExpiresAt.ToDateTimeOffset().ToString("u");

        var body = $"""
            Hi {@event.FirstName},

            You've been invited to join {tenant.Name}.

            Accept your invitation:
            {invitationUrl}

            This invitation expires at {expiresAt}.
            """;

        await emailSender.SendEmailAsync(@event.Email, subject, body, tenant.DefaultSenderEmail, ct);
    }
}
