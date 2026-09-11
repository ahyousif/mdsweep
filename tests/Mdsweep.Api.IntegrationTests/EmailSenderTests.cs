using Mdsweep.Infrastructure.Email;
using MimeKit;

namespace Mdsweep.Api.IntegrationTests;

public sealed class EmailSenderTests
{
    [Fact]
    public void Html_email_uses_an_html_body()
    {
        const string body = "<p>Accept invitation</p>";

        var message = EmailSender.CreateMessage(
            "invitee@example.test",
            "Invitation",
            body,
            "no-reply@mdsweep.com",
            isHtml: true
        );

        var part = Assert.IsType<TextPart>(message.Body);
        Assert.Equal("html", part.ContentType.MediaSubtype);
        Assert.Equal(body, part.Text);
    }

    [Fact]
    public void Plain_text_email_remains_plain_text()
    {
        const string body = "Plain text message";

        var message = EmailSender.CreateMessage(
            "invitee@example.test",
            "Message",
            body,
            "no-reply@mdsweep.com",
            isHtml: false
        );

        var part = Assert.IsType<TextPart>(message.Body);
        Assert.Equal("plain", part.ContentType.MediaSubtype);
        Assert.Equal(body, part.Text);
    }
}
