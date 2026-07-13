using DotMailer.Core;

namespace DotMailer.AzureCommunication;

public sealed class AzureCommunicationEmailTransport : IEmailTransport
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
