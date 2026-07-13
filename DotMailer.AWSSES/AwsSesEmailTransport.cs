using DotMailer.Core;

namespace DotMailer.AWSSES;

public sealed class AwsSesEmailTransport : IEmailTransport
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
