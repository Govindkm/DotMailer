namespace DotMailer.Core;

/// <summary>
/// High-level client for composing and sending emails.
/// Implementations delegate delivery to an <see cref="IEmailTransport"/>.
/// </summary>
public interface IEmailClient
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
