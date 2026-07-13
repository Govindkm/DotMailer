using DotMailer.Core;

namespace DotMailer.SMTP;

/// <summary>
/// SMTP transport implementation using MailKit.
/// </summary>
public sealed class SmtpEmailTransport : IEmailTransport
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
