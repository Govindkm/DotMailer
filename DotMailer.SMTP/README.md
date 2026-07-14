# Govindkm.DotMailer.SMTP

Custom SMTP email transport for DotMailer with support for STARTTLS encryption and AUTH LOGIN authentication.

## Overview

`Govindkm.DotMailer.SMTP` is a pure .NET SMTP transport implementation with NO external dependencies. It enables sending emails through any SMTP server (Gmail, SendGrid, Mailgun, your own mail server, etc.).

## Features

- ✅ Direct SMTP protocol implementation (RFC 5321)
- ✅ STARTTLS encryption support
- ✅ AUTH LOGIN authentication
- ✅ MIME multipart email support with attachments
- ✅ No external dependencies - pure .NET Sockets and SSL/TLS
- ✅ Async/await support throughout
- ✅ Comprehensive error handling and logging

## Installation

```bash
dotnet add package Govindkm.DotMailer.SMTP
```

Also add the core package:

```bash
dotnet add package Govindkm.DotMailer.Core
```

## Configuration

### appsettings.json

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseSsl": false,
    "UseStartTls": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "TimeoutMs": 30000
  }
}
```

### Program.cs (Dependency Injection)

```csharp
using Govindkm.DotMailer.Extensions.DependencyInjection;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add DotMailer with SMTP transport
builder.Services.AddDotMailer();

var app = builder.Build();
```

## Usage

### Basic Email Sending

```csharp
using Govindkm.DotMailer.Core;

public class EmailService
{
    private readonly IEmailClient _emailClient;

    public EmailService(IEmailClient emailClient)
    {
        _emailClient = emailClient;
    }

    public async Task SendWelcomeEmail(string recipientEmail)
    {
        var message = new EmailMessage
        {
            From = new EmailAddress("noreply@example.com", "Example App"),
            To = new[] { new EmailAddress(recipientEmail) },
            Subject = "Welcome!",
            HtmlBody = "<h1>Welcome to our application!</h1>",
            TextBody = "Welcome to our application!"
        };

        var result = await _emailClient.SendAsync(message);
        
        if (result.IsSuccess)
        {
            Console.WriteLine($"Email sent: {result.MessageId}");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }
}
```

### With Attachments

```csharp
var fileContent = System.IO.File.ReadAllBytes("document.pdf");
var message = new EmailMessage
{
    From = new EmailAddress("sender@example.com"),
    Subject = "Document",
    HtmlBody = "<p>Please see attached document</p>"
};

message.To.Add(new EmailAddress("recipient@example.com"));
message.Attachments.Add(new EmailAttachment("document.pdf", fileContent, "application/pdf"));

var result = await _emailClient.SendAsync(message);
```

### With CC and BCC

```csharp
message.Cc.Add(new EmailAddress("cc@example.com"));
message.Bcc.Add(new EmailAddress("bcc@example.com"));
```

## Supported SMTP Connection Modes

| Mode | Port | Configuration | Use Case |
|------|------|---------------|----------|
| **STARTTLS** | 587 | `UseStartTls = true` | Standard, recommended for most services |
| **SSL/TLS** | 465 | `UseSsl = true` | Direct encryption connection |
| **Plain** | 25 | Neither flag | Rarely used, insecure |

## Supported SMTP Servers

This implementation works with any standard SMTP provider:

- **Gmail** - `smtp.gmail.com:587` with STARTTLS
- **Outlook/Office 365** - `smtp.office365.com:587` with STARTTLS
- **SendGrid** - `smtp.sendgrid.net:587` with STARTTLS
- **Mailgun** - `smtp.mailgun.org:587` with STARTTLS
- **AWS SES** - `email-smtp.{region}.amazonaws.com:587` with STARTTLS
- **Any custom mail server** - Configure host, port, and credentials

## Configuration Options

| Setting | Type | Required | Default | Description |
|---------|------|----------|---------|-------------|
| `Host` | string | ✅ | — | SMTP server hostname |
| `Port` | int | ✅ | 587 | SMTP server port |
| `Username` | string | ✅ | — | SMTP authentication username |
| `Password` | string | ✅ | — | SMTP authentication password |
| `UseSsl` | bool | ❌ | false | Use SSL/TLS from start (port 465) |
| `UseStartTls` | bool | ❌ | true | Upgrade to TLS (port 587) |
| `TimeoutMs` | int | ❌ | 30000 | Connection timeout in milliseconds |

## How It Works

The SMTP protocol flow:

1. **Connect** - Establish TCP connection to SMTP server, receive 220 banner
2. **EHLO** - Send Extended HELO to negotiate capabilities
3. **STARTTLS** - Upgrade connection to TLS (if configured)
4. **AUTH** - Authenticate with AUTH LOGIN mechanism
5. **MAIL FROM** - Specify sender address
6. **RCPT TO** - Specify recipient addresses (To, Cc, Bcc)
7. **DATA** - Begin message transmission
8. **Message** - Send complete MIME-formatted email
9. **QUIT** - Gracefully close connection

All communication uses CRLF line endings per RFC 5321.

## Error Handling

```csharp
var result = await _emailClient.SendAsync(message);

if (!result.IsSuccess)
{
    logger.LogError("Email failed: {Error}", result.ErrorMessage);
    
    // Common error patterns
    if (result.ErrorMessage.Contains("authentication"))
        Console.WriteLine("Check your username and password");
    else if (result.ErrorMessage.Contains("reserved"))
        Console.WriteLine("Package ID is reserved on NuGet - use a different name");
    else if (result.ErrorMessage.Contains("timeout"))
        Console.WriteLine("SMTP server not responding");
}
```

## Logging

Enable logging to see detailed SMTP operations:

```csharp
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

You'll see logs showing connection, authentication, and sending details.

## Security Best Practices

1. **Never hardcode credentials** in source code
2. **Use secrets management** for sensitive data:
   ```csharp
   var username = configuration["SmtpSettings:Username"];
   var password = configuration["SmtpSettings:Password"];
   ```
3. **Use app-specific passwords** for Gmail (not your main password)
4. **Always use TLS/SSL** - never send credentials over plain text
5. **Validate email addresses** before sending
6. **Rate limit** emails to avoid being flagged as spam

## Validation

The transport validates messages before sending:

- **From address**: Required in message or must be configured
- **To recipients**: At least one must be specified
- **Subject**: Cannot be empty
- **Body**: Either text or HTML body (or both) required

## Common Issues

| Problem | Solution |
|---------|----------|
| Authentication failed | Verify credentials, check if app-specific password is needed |
| Connection timeout | Ensure host/port are correct, check firewall rules |
| SSL/TLS error | Use correct settings: 587 with STARTTLS or 465 with SSL |
| Package ID reserved | Use scoped name like `YourName.DotMailer.SMTP` |
| Message validation failed | Ensure all required fields are set |

## License

MIT License - See LICENSE file in repository for details

## More Information

For complete documentation and examples:
[DotMailer GitHub Repository](https://github.com/Govindkm/DotMailer)
