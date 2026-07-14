namespace DotMailer.Core;

/// <summary>
/// Default implementation of IEmailClient that delegates to IEmailTransport.
/// </summary>
public sealed class DefaultEmailClient : IEmailClient
{
    private readonly IEmailTransport _transport;

    public DefaultEmailClient(IEmailTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
    }

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _transport.SendAsync(message, cancellationToken);
    }
}
