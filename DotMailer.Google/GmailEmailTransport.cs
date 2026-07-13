using DotMailer.Core;

namespace DotMailer.Google;

public sealed class GmailEmailTransport : IEmailTransport
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
