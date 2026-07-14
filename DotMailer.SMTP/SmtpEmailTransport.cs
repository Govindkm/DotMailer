using DotMailer.Core;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace DotMailer.SMTP;

/// <summary>
/// SMTP transport implementation using MailKit.
/// Sends emails via SMTP protocol to any SMTP server.
/// </summary>
public sealed class SmtpEmailTransport : IEmailTransport
{
    private readonly SmtpTransportOptions _options;
    private readonly ILogger<SmtpEmailTransport>? _logger;

    public SmtpEmailTransport(SmtpTransportOptions options, ILogger<SmtpEmailTransport>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidateMessage(message);

        try
        {
            var mimeMessage = ConvertToMimeMessage(message);
            string messageId;

            using (var client = new SmtpClient())
            {
                _logger?.LogInformation("Connecting to SMTP server at {Host}:{Port}", _options.Host, _options.Port);

                // Connect to SMTP server
                await client.ConnectAsync(
                    _options.Host,
                    _options.Port,
                    _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                    cancellationToken);

                // Authenticate
                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    _logger?.LogInformation("Authenticating as {Username}", _options.Username);
                    await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
                }

                // Send message
                _logger?.LogInformation("Sending email to {Recipients}", 
                    string.Join(", ", message.To.Select(r => r.Address)));
                
                messageId = await client.SendAsync(mimeMessage, cancellationToken);

                // Disconnect
                await client.DisconnectAsync(true, cancellationToken);
            }

            _logger?.LogInformation("Email sent successfully with message ID: {MessageId}", messageId);
            return EmailSendResult.Success(messageId);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to send email via SMTP: {ex.Message}";
            _logger?.LogError(ex, errorMessage);
            return EmailSendResult.Failure(errorMessage);
        }
    }

    private MimeMessage ConvertToMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // From address
        var fromAddress = message.From ?? 
            new EmailAddress(
                _options.DefaultFromAddress ?? throw new InvalidOperationException("No From address provided"),
                _options.DefaultFromDisplayName);
        
        mimeMessage.From.Add(new MailboxAddress(fromAddress.DisplayName, fromAddress.Address));

        // To recipients
        foreach (var recipient in message.To)
        {
            mimeMessage.To.Add(new MailboxAddress(recipient.DisplayName, recipient.Address));
        }

        // CC recipients
        foreach (var recipient in message.Cc)
        {
            mimeMessage.Cc.Add(new MailboxAddress(recipient.DisplayName, recipient.Address));
        }

        // BCC recipients
        foreach (var recipient in message.Bcc)
        {
            mimeMessage.Bcc.Add(new MailboxAddress(recipient.DisplayName, recipient.Address));
        }

        // Reply-To
        if (message.ReplyTo is not null)
        {
            mimeMessage.ReplyTo.Add(new MailboxAddress(message.ReplyTo.DisplayName, message.ReplyTo.Address));
        }

        // Subject
        mimeMessage.Subject = message.Subject;

        // Body
        var bodyBuilder = new BodyBuilder();

        if (!string.IsNullOrEmpty(message.TextBody))
            bodyBuilder.TextBody = message.TextBody;

        if (!string.IsNullOrEmpty(message.HtmlBody))
            bodyBuilder.HtmlBody = message.HtmlBody;

        // Attachments
        foreach (var attachment in message.Attachments)
        {
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, new ContentType("application", "octet-stream"));
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        // Custom headers
        foreach (var header in message.Headers)
        {
            mimeMessage.Headers[header.Key] = header.Value;
        }

        return mimeMessage;
    }

    private void ValidateMessage(EmailMessage message)
    {
        if (message.From is null && string.IsNullOrWhiteSpace(_options.DefaultFromAddress))
            throw new InvalidOperationException("EmailMessage must have a From address or DefaultFromAddress must be configured");

        if (!message.To.Any())
            throw new InvalidOperationException("EmailMessage must have at least one To recipient");

        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new InvalidOperationException("EmailMessage must have a Subject");

        if (string.IsNullOrEmpty(message.TextBody) && string.IsNullOrEmpty(message.HtmlBody))
            throw new InvalidOperationException("EmailMessage must have either TextBody or HtmlBody");
    }
}
