namespace DotMailer.Core;

/// <summary>
/// Represents a file attachment to be included in an email.
/// </summary>
public sealed class EmailAttachment
{
    public string FileName { get; }
    public byte[] Content { get; }
    public string ContentType { get; }

    public EmailAttachment(string fileName, byte[] content, string contentType = "application/octet-stream")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        FileName = fileName;
        Content = content;
        ContentType = contentType;
    }
}
