using Mdsweep.Application.Common.Email;

namespace Mdsweep.Infrastructure.Email;

public sealed class EmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        string? from = null,
        CancellationToken ct = default
    )
    {
        var email = new MimeMessage();

        email.From.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(from) ? options.Value.From : from));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new BodyBuilder { TextBody = body }.ToMessageBody();

        using var client = new SmtpClient();
        var secureSocketOptions = options.Value.UseStartTls
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        await client.ConnectAsync(options.Value.Host, options.Value.Port, secureSocketOptions, ct);

        if (!string.IsNullOrEmpty(options.Value.Username))
        {
            await client.AuthenticateAsync(options.Value.Username, options.Value.Password!, ct);
        }

        await client.SendAsync(email, ct);
        await client.DisconnectAsync(true, ct);
    }
}
