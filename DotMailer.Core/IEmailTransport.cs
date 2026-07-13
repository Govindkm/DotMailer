namespace DotMailer.Core;

/// <summary>
/// Low-level abstraction responsible for delivering an email message
/// using a specific protocol or provider (e.g. SMTP, SendGrid, SES).
/// </summary>
public interface IEmailTransport
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
