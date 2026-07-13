using DotMailer.Core;

namespace DotMailer.Postmark;

public sealed class PostmarkEmailTransport : IEmailTransport
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
