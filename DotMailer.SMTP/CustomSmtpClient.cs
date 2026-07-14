using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace DotMailer.SMTP;

/// <summary>
/// Custom SMTP client implementation for sending emails via SMTP protocol.
/// Supports STARTTLS encryption and LOGIN/PLAIN authentication.
/// </summary>
public sealed class SmtpClient : IDisposable
{
    private TcpClient? _tcpClient;
    private SslStream? _sslStream;
    private StreamReader? _reader;
    private bool _disposed;
    private ILogger? _logger;

    public SmtpClient(ILogger? logger = null)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string host, int port, bool useSsl = false, int timeoutMs = 10000)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SmtpClient));

        _tcpClient = new TcpClient();
        _tcpClient.ReceiveTimeout = timeoutMs;
        _tcpClient.SendTimeout = timeoutMs;

        await _tcpClient.ConnectAsync(host, port);

        if (useSsl)
        {
            _sslStream = new SslStream(_tcpClient.GetStream(), false, (s, cert, chain, errors) => true);
            await _sslStream.AuthenticateAsClientAsync(host);
            _reader = new StreamReader(_sslStream, Encoding.UTF8);
        }
        else
        {
            _reader = new StreamReader(_tcpClient.GetStream(), Encoding.UTF8);
        }

        // Read initial banner
        var response = await ReadResponseAsync();
        if (!response.StartsWith("220"))
        {
            throw new SmtpException($"SMTP server returned unexpected response: {response}");
        }

        // Send EHLO to identify as ESMTP client (required for STARTTLS and other extensions)
        // Gmail requires EHLO before STARTTLS per SMTP RFC 5321
        // IMPORTANT: SMTP requires CRLF (\r\n) line endings per RFC 5321
        await WriteSMTPCommandAsync("EHLO [127.0.0.1]");
        response = await ReadResponseAsync();
        if (!response.StartsWith("250"))
        {
            throw new SmtpException($"EHLO command failed: {response}");
        }
    }

    public async Task StartTlsAsync(string host)
    {
        if (_reader == null)
            throw new InvalidOperationException("Not connected");

        // Send STARTTLS command - SMTP requires CRLF
        await WriteSMTPCommandAsync("STARTTLS");
        var response = await ReadResponseAsync();

        if (!response.StartsWith("220"))
        {
            throw new SmtpException($"STARTTLS failed: {response}");
        }

        // Upgrade connection to SSL
        if (_tcpClient?.GetStream() is NetworkStream networkStream)
        {
            _sslStream = new SslStream(networkStream, false, (s, cert, chain, errors) => true);
            await _sslStream.AuthenticateAsClientAsync(host);

            // Replace reader with SSL stream
            _reader = new StreamReader(_sslStream, Encoding.UTF8);

            // Send EHLO again after TLS upgrade (RFC 3207)
            await WriteSMTPCommandAsync("EHLO localhost");
            response = await ReadResponseAsync();
            if (!response.StartsWith("250"))
            {
                throw new SmtpException($"EHLO after STARTTLS failed: {response}");
            }
        }
    }

    public async Task AuthenticateAsync(string username, string password)
    {
        if (_reader == null)
            throw new InvalidOperationException("Not connected");

        // Send AUTH LOGIN - SMTP requires CRLF
        await WriteSMTPCommandAsync("AUTH LOGIN");
        var response = await ReadResponseAsync();

        if (!response.StartsWith("334"))
        {
            throw new SmtpException($"AUTH LOGIN failed: {response}");
        }

        // Send username (base64 encoded)
        var userBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));
        await WriteSMTPCommandAsync(userBase64);
        response = await ReadResponseAsync();

        if (!response.StartsWith("334"))
        {
            throw new SmtpException($"Username rejected: {response}");
        }

        // Send password (base64 encoded)
        var passBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
        await WriteSMTPCommandAsync(passBase64);
        response = await ReadResponseAsync();

        if (!response.StartsWith("235"))
        {
            throw new SmtpException($"Authentication failed: {response}");
        }
    }

    public async Task<string> SendAsync(string fromEmail, string[] toEmails, string[] ccEmails, string[] bccEmails, string subject, string textBody, string htmlBody)
    {
        if (_reader == null)
            throw new InvalidOperationException("Not connected");

        // MAIL FROM
        await WriteSMTPCommandAsync($"MAIL FROM:<{fromEmail}>");
        var response = await ReadResponseAsync();
        if (!response.StartsWith("250"))
        {
            throw new SmtpException($"MAIL FROM failed: {response}");
        }

        // RCPT TO for all recipients
        var allRecipients = toEmails.Concat(ccEmails).Concat(bccEmails).Distinct().ToArray();
        foreach (var recipient in allRecipients)
        {
            await WriteSMTPCommandAsync($"RCPT TO:<{recipient}>");
            response = await ReadResponseAsync();
            if (!response.StartsWith("250"))
            {
                throw new SmtpException($"RCPT TO failed for {recipient}: {response}");
            }
        }

        // DATA
        await WriteSMTPCommandAsync("DATA");
        response = await ReadResponseAsync();
        if (!response.StartsWith("354"))
        {
            throw new SmtpException($"DATA command failed: {response}");
        }

        // Build and send MIME message
        var mimeMessage = BuildMimeMessage(fromEmail, toEmails, ccEmails, bccEmails, subject, textBody, htmlBody);
        var mimeBytes = Encoding.UTF8.GetBytes(mimeMessage);
        if (_sslStream != null)
        {
            await _sslStream.WriteAsync(mimeBytes);
            await _sslStream.FlushAsync();
        }
        else if (_tcpClient?.GetStream() is Stream stream)
        {
            await stream.WriteAsync(mimeBytes);
            await stream.FlushAsync();
        }
        await WriteSMTPCommandAsync(".");

        response = await ReadResponseAsync();
        if (!response.StartsWith("250"))
        {
            throw new SmtpException($"Message rejected: {response}");
        }

        // Extract message ID from response
        var messageId = ExtractMessageId(response);
        return messageId ?? "unknown";
    }

    public async Task DisconnectAsync()
    {
        if (_reader == null) return;

        try
        {
            await WriteSMTPCommandAsync("QUIT");
            await ReadResponseAsync();
        }
        catch
        {
            // Ignore errors during disconnect
        }
        finally
        {
            Dispose();
        }
    }

    private async Task<string> ReadResponseAsync()
    {
        if (_reader == null)
            throw new InvalidOperationException("Not connected");

        var response = new StringBuilder();
        string? line;

        while ((line = await _reader.ReadLineAsync()) != null)
        {
            response.AppendLine(line);
            _logger?.LogDebug("<<< Received SMTP response: {Response}", line);

            // Check if this is the last line (no hyphen after code)
            if (line.Length >= 4 && line[3] != '-')
            {
                break;
            }
        }

        return response.ToString().Trim();
    }

    private string BuildMimeMessage(string from, string[] to, string[] cc, string[] bcc, string subject, string textBody, string htmlBody)
    {
        var sb = new StringBuilder();

        // SMTP DATA section requires CRLF line endings per RFC 5321
        const string CRLF = "\r\n";

        // Headers
        sb.Append($"From: {from}").Append(CRLF);
        sb.Append($"To: {string.Join(", ", to)}").Append(CRLF);

        if (cc.Length > 0)
        {
            sb.Append($"Cc: {string.Join(", ", cc)}").Append(CRLF);
        }

        sb.Append($"Subject: {EncodeSubject(subject)}").Append(CRLF);
        sb.Append($"Date: {DateTime.UtcNow:R}").Append(CRLF);
        sb.Append($"Message-ID: <{Guid.NewGuid()}@dotmailer>").Append(CRLF);
        sb.Append("MIME-Version: 1.0").Append(CRLF);

        // Content type for multipart
        var boundary = $"boundary_{Guid.NewGuid():N}";
        sb.Append($"Content-Type: multipart/alternative; boundary=\"{boundary}\"").Append(CRLF);
        sb.Append(CRLF);

        // Text part
        if (!string.IsNullOrEmpty(textBody))
        {
            sb.Append($"--{boundary}").Append(CRLF);
            sb.Append("Content-Type: text/plain; charset=\"utf-8\"").Append(CRLF);
            sb.Append("Content-Transfer-Encoding: 8bit").Append(CRLF);
            sb.Append(CRLF);
            sb.Append(textBody).Append(CRLF);
            sb.Append(CRLF);
        }

        // HTML part
        if (!string.IsNullOrEmpty(htmlBody))
        {
            sb.Append($"--{boundary}").Append(CRLF);
            sb.Append("Content-Type: text/html; charset=\"utf-8\"").Append(CRLF);
            sb.Append("Content-Transfer-Encoding: 8bit").Append(CRLF);
            sb.Append(CRLF);
            sb.Append(htmlBody).Append(CRLF);
            sb.Append(CRLF);
        }

        sb.Append($"--{boundary}--").Append(CRLF);

        return sb.ToString();
    }

    private string EncodeSubject(string subject)
    {
        // Simple UTF-8 encoding for subject line
        if (subject.Any(c => c > 127))
        {
            var bytes = Encoding.UTF8.GetBytes(subject);
            return $"=?UTF-8?B?{Convert.ToBase64String(bytes)}?=";
        }

        return subject;
    }

    private string? ExtractMessageId(string response)
    {
        // Try to extract message ID from SMTP response
        var lines = response.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("OK") || line.Contains("queued"))
            {
                return line.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Write an SMTP command with proper CRLF line endings per RFC 5321.
    /// </summary>
    private async Task WriteSMTPCommandAsync(string command)
    {
        if (_tcpClient == null)
            throw new InvalidOperationException("Not connected");

        // SMTP requires CRLF (\r\n) line endings per RFC 5321
        _logger?.LogInformation(">>> [WRITE_SMTP_COMMAND_V2] Sending: {Command}", command);
        
        // Write bytes directly to ensure CRLF is sent correctly
        var bytes = Encoding.UTF8.GetBytes(command + "\r\n");
        if (_sslStream != null)
        {
            await _sslStream.WriteAsync(bytes);
            await _sslStream.FlushAsync();
        }
        else
        {
            var networkStream = _tcpClient.GetStream();
            await networkStream.WriteAsync(bytes);
            await networkStream.FlushAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _reader?.Dispose();
        _sslStream?.Dispose();
        _tcpClient?.Dispose();

        _disposed = true;
    }
}

/// <summary>
/// Exception thrown for SMTP protocol errors.
/// </summary>
public class SmtpException : Exception
{
    public SmtpException(string message) : base(message) { }
    public SmtpException(string message, Exception innerException) : base(message, innerException) { }
}
