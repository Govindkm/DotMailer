namespace DotMailer.Core;

/// <summary>
/// Represents a complete email message.
/// </summary>
public sealed class EmailMessage
{
    public EmailAddress From { get; set; } = default!;
    public IList<EmailAddress> To { get; set; } = new List<EmailAddress>();
    public IList<EmailAddress> Cc { get; set; } = new List<EmailAddress>();
    public IList<EmailAddress> Bcc { get; set; } = new List<EmailAddress>();
    public string Subject { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public EmailAddress? ReplyTo { get; set; }
    public IList<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}
