using System.Text.Encodings.Web;
using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Configuration;
using Mdsweep.Application.Common.Email;
using Mdsweep.Domain.Tenants;
using Mdsweep.Domain.Users.Events;
using Microsoft.Extensions.Options;

namespace Mdsweep.Application.Users.Invitations.DomainEventHandlers;

public sealed class SendEmailWhenInvitationCreatedHandler(
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

        var firstName = HtmlEncoder.Default.Encode(@event.FirstName);
        var tenantName = HtmlEncoder.Default.Encode(tenant.Name);
        var encodedInvitationUrl = HtmlEncoder.Default.Encode(invitationUrl);

        var body = $"""
            <!doctype html>
            <html>
            <body style="font-family: Arial, sans-serif; color: #17211f;">
                <p>Hi {firstName},</p>

                <p>
                    You've been invited to join <strong>{tenantName}</strong> on MDSweep.
                </p>

                <p style="margin: 28px 0;">
                    <a
                        href="{encodedInvitationUrl}"
                        style="
                            display: inline-block;
                            padding: 12px 20px;
                            background: #177d68;
                            color: #ffffff;
                            text-decoration: none;
                            border-radius: 6px;
                            font-weight: 600;
                        "
                    >
                        Accept invitation
                    </a>
                </p>

                <p style="color: #5f6b68; font-size: 14px;">
                    This invitation expires on {expiresAt}.
                </p>
            </body>
            </html>
            """;

        await emailSender.SendEmailAsync(
            @event.Email,
            subject,
            body,
            tenant.DefaultSenderEmail,
            isHtml: true,
            ct: ct
        );
    }
}
