using DotMailer.Core;

namespace DotMailer.Mailgun;

public sealed class MailgunEmailTransport : IEmailTransport
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
