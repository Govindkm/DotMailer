namespace DotMailer.Core;

/// <summary>
/// Represents an email address with an optional display name.
/// </summary>
public sealed class EmailAddress
{
    public string Address { get; }
    public string? DisplayName { get; }

    public EmailAddress(string address, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        Address = address;
        DisplayName = displayName;
    }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(DisplayName)
            ? Address
            : $"{DisplayName} <{Address}>";
}
