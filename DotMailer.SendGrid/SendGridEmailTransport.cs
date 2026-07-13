using DotMailer.Core;

namespace DotMailer.SendGrid;

public sealed class SendGridEmailTransport : IEmailTransport
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
