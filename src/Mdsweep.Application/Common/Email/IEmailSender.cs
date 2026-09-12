namespace Mdsweep.Application.Common.Email;

public interface IEmailSender
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        string? from = null,
        bool isHtml = false,
        CancellationToken ct = default
    );
}
