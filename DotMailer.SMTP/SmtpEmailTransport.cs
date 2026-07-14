using DotMailer.Core;
using Microsoft.Extensions.Logging;

namespace DotMailer.SMTP;

/// <summary>
/// SMTP transport implementation using a custom SMTP client.
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
            using var client = new SmtpClient(_logger);

            _logger?.LogInformation("Connecting to SMTP server at {Host}:{Port}", _options.Host, _options.Port);

            // Determine SSL/TLS settings
            bool useSsl = _options.UseSsl;

            // Connect to SMTP server
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                useSsl,
                _options.ConnectionTimeout);

            // Upgrade to TLS if requested (STARTTLS)
            if (_options.UseStartTls && !useSsl)
            {
                _logger?.LogInformation("Upgrading connection to TLS with STARTTLS");
                await client.StartTlsAsync(_options.Host);
            }

            // Authenticate
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                _logger?.LogInformation("Authenticating as {Username}", _options.Username);
                await client.AuthenticateAsync(_options.Username, _options.Password);
            }

            // Get sender address
            var fromAddress = message.From?.Address ??
                _options.DefaultFromAddress ??
                throw new InvalidOperationException("No From address provided");

            // Get recipient arrays
            var toEmails = message.To.Select(r => r.Address).ToArray();
            var ccEmails = message.Cc.Select(r => r.Address).ToArray();
            var bccEmails = message.Bcc.Select(r => r.Address).ToArray();

            // Build message bodies
            var textBody = message.TextBody ?? string.Empty;
            var htmlBody = message.HtmlBody ?? string.Empty;

            // Send message
            _logger?.LogInformation("Sending email to {Recipients}",
                string.Join(", ", toEmails));

            var messageId = await client.SendAsync(
                fromAddress,
                toEmails,
                ccEmails,
                bccEmails,
                message.Subject,
                textBody,
                htmlBody);

            // Disconnect
            await client.DisconnectAsync();

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
