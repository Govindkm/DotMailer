namespace DotMailer.SMTP;

/// <summary>
/// Configuration options for SMTP transport.
/// </summary>
public sealed class SmtpTransportOptions
{
    /// <summary>
    /// SMTP server hostname (e.g., "smtp.gmail.com", "smtp.sendgrid.net").
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 25, 465 for SSL, 587 for TLS).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Username for SMTP authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password or API key for SMTP authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Default sender email address if not specified in EmailMessage.
    /// </summary>
    public string? DefaultFromAddress { get; set; }

    /// <summary>
    /// Default sender display name.
    /// </summary>
    public string? DefaultFromDisplayName { get; set; }

    /// <summary>
    /// Use SSL/TLS connection (true for port 465).
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Use STARTTLS for connection upgrade (true for port 587).
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 10000;

    /// <summary>
    /// Validate configuration and throw if invalid.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new ArgumentException("Host is required", nameof(Host));

        if (Port <= 0 || Port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(Port));

        if (string.IsNullOrWhiteSpace(Username))
            throw new ArgumentException("Username is required", nameof(Username));

        if (string.IsNullOrWhiteSpace(Password))
            throw new ArgumentException("Password is required", nameof(Password));
    }
}
