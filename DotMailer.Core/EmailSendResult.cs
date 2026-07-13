namespace DotMailer.Core;

/// <summary>
/// Result returned after an email send attempt.
/// </summary>
public sealed class EmailSendResult
{
    public bool IsSuccess { get; }
    public string? MessageId { get; }
    public string? ErrorMessage { get; }

    private EmailSendResult(bool isSuccess, string? messageId, string? errorMessage)
    {
        IsSuccess = isSuccess;
        MessageId = messageId;
        ErrorMessage = errorMessage;
    }

    public static EmailSendResult Success(string? messageId = null) =>
        new(true, messageId, null);

    public static EmailSendResult Failure(string errorMessage) =>
        new(false, null, errorMessage);
}
