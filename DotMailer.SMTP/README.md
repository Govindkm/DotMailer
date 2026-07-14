# DotMailer.SMTP - SMTP Email Transport

## Overview

`DotMailer.SMTP` is an SMTP transport implementation for DotMailer using [MailKit](https://github.com/jstedfast/MailKit). It enables sending emails through any SMTP server (Gmail, SendGrid, Mailgun, your own mail server, etc.).

## How SMTP Transport Works

SMTP (Simple Mail Transfer Protocol) is the standard protocol for sending emails:

1. **Connection**: Connects to an SMTP server using TCP socket
2. **Authentication**: Authenticates with username/password or API credentials
3. **Message Conversion**: Converts the `EmailMessage` to MIME format
4. **Transmission**: Sends the email through the SMTP protocol
5. **Response**: Returns a message ID (if successful) or error details
6. **Disconnection**: Gracefully closes the connection

### Supported Connection Modes

- **Port 25 (Plain)**: Unencrypted connection (rarely used)
- **Port 465 (SMTPS)**: SSL/TLS encryption from the start (`UseSsl = true`)
- **Port 587 (SMTP+STARTTLS)**: Plain connection that upgrades to TLS (`UseStartTls = true`)

## Installation

Add the package to your project:

```bash
dotnet add package DotMailer.SMTP
```

Also ensure you have the main DotMailer package and its DI extension:

```bash
dotnet add package DotMailer.Core
dotnet add package DotMailer.Extensions.DependencyInjection
```

## Setup & Configuration

### Option 1: Programmatic Configuration (Recommended)

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotMailer.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDotMailer(builder =>
{
    builder.AddSmtpTransport(options =>
    {
        options.Host = "smtp.gmail.com";
        options.Port = 587;
        options.Username = "your-email@gmail.com";
        options.Password = "your-app-password";  // Use app-specific password for Gmail
        options.DefaultFromAddress = "your-email@gmail.com";
        options.DefaultFromDisplayName = "Your Name";
        options.UseStartTls = true;  // For port 587
    });
});

var serviceProvider = services.BuildServiceProvider();
```

### Option 2: Configuration from appsettings.json

**appsettings.json:**
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "DefaultFromAddress": "your-email@gmail.com",
    "DefaultFromDisplayName": "Your Name",
    "UseStartTls": true,
    "ConnectionTimeout": 10000
  }
}
```

**Program.cs:**
```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddDotMailer(b =>
    b.AddSmtpTransport(config, "Smtp")  // Section name defaults to "Smtp"
);
```

## Usage

### Basic Email Sending

```csharp
using DotMailer.Core;
using Microsoft.Extensions.DependencyInjection;

// Inject IEmailClient
var emailClient = serviceProvider.GetRequiredService<IEmailClient>();

// Create and send email
var message = new EmailMessage
{
    From = new EmailAddress("sender@example.com", "Sender Name"),
    Subject = "Welcome!",
    HtmlBody = "<h1>Hello</h1><p>Welcome to our service.</p>",
    TextBody = "Hello\n\nWelcome to our service."
};

message.To.Add(new EmailAddress("recipient@example.com", "Recipient Name"));

var result = await emailClient.SendAsync(message);

if (result.IsSuccess)
{
    Console.WriteLine($"Email sent! Message ID: {result.MessageId}");
}
else
{
    Console.WriteLine($"Failed to send email: {result.ErrorMessage}");
}
```

### Advanced Features

#### With CC and BCC

```csharp
var message = new EmailMessage
{
    From = new EmailAddress("sender@example.com"),
    Subject = "Team Update",
    HtmlBody = "<p>Here's the latest update...</p>"
};

message.To.Add(new EmailAddress("manager@example.com"));
message.Cc.Add(new EmailAddress("colleague1@example.com"));
message.Cc.Add(new EmailAddress("colleague2@example.com"));
message.Bcc.Add(new EmailAddress("archive@example.com"));  // Hidden recipient
```

#### With Attachments

```csharp
var fileContent = await System.IO.File.ReadAllBytesAsync("report.pdf");
message.Attachments.Add(new EmailAttachment(
    "report.pdf",
    fileContent,
    "application/pdf"
));
```

#### With Reply-To Address

```csharp
message.ReplyTo = new EmailAddress("support@example.com", "Support Team");
```

#### With Custom Headers

```csharp
message.Headers["X-Custom-Header"] = "CustomValue";
message.Headers["X-Priority"] = "1";  // High priority
```

#### With Tags (for tracking)

```csharp
message.Tags["campaign"] = "welcome-series";
message.Tags["version"] = "v2";
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Host` | string | — | SMTP server hostname (required) |
| `Port` | int | 587 | SMTP server port |
| `Username` | string | — | SMTP authentication username (required) |
| `Password` | string | — | SMTP authentication password (required) |
| `DefaultFromAddress` | string | null | Default sender email if not in message |
| `DefaultFromDisplayName` | string | null | Default sender display name |
| `UseSsl` | bool | false | Use SSL/TLS from start (port 465) |
| `UseStartTls` | bool | true | Use STARTTLS upgrade (port 587) |
| `ConnectionTimeout` | int | 10000 | Connection timeout in milliseconds |

## Common SMTP Server Configurations

### Gmail

```csharp
options.Host = "smtp.gmail.com";
options.Port = 587;
options.UseStartTls = true;
options.Username = "your-email@gmail.com";
options.Password = "your-app-password";  // Generate at myaccount.google.com/apppasswords
```

### SendGrid

```csharp
options.Host = "smtp.sendgrid.net";
options.Port = 587;
options.UseStartTls = true;
options.Username = "apikey";  // Always "apikey"
options.Password = "SG.xxxxxxxxxxxxx";  // Your SendGrid API key
```

### Mailgun

```csharp
options.Host = "smtp.mailgun.org";
options.Port = 587;
options.UseStartTls = true;
options.Username = "postmaster@your-domain.mailgun.org";
options.Password = "your-smtp-password";
```

### AWS SES

```csharp
options.Host = "email-smtp.region.amazonaws.com";  // e.g., email-smtp.us-east-1.amazonaws.com
options.Port = 587;
options.UseStartTls = true;
options.Username = "your-smtp-username";
options.Password = "your-smtp-password";
```

### Custom Mail Server

```csharp
options.Host = "mail.yourdomain.com";
options.Port = 587;  // or 465 for SSL
options.UseStartTls = true;  // or UseSsl = true for port 465
options.Username = "user@yourdomain.com";
options.Password = "password";
```

## Error Handling

```csharp
var result = await emailClient.SendAsync(message);

if (!result.IsSuccess)
{
    // Log the error
    logger.LogError("Email send failed: {Error}", result.ErrorMessage);
    
    // Handle specific scenarios
    if (result.ErrorMessage.Contains("authentication"))
    {
        // Check credentials
    }
    else if (result.ErrorMessage.Contains("timeout"))
    {
        // Server unreachable or slow
    }
    else if (result.ErrorMessage.Contains("recipient"))
    {
        // Invalid recipient address
    }
}
```

## Validation Rules

The SMTP transport validates messages before sending:

- **From address required**: Either in `EmailMessage.From` or configured `DefaultFromAddress`
- **At least one To recipient**: `message.To` must have at least one address
- **Subject required**: Cannot be empty
- **Body required**: Either `TextBody` or `HtmlBody` (or both) must be set
- **Configuration required**: Host, Port, Username, and Password must be set and valid

## Logging

The SMTP transport supports structured logging via `Microsoft.Extensions.Logging`:

```csharp
builder.Logging.AddConsole();  // Enable logging to see SMTP operations

// You'll see logs like:
// "Connecting to SMTP server at smtp.gmail.com:587"
// "Authenticating as your-email@gmail.com"
// "Sending email to recipient@example.com"
// "Email sent successfully with message ID: ..."
```

## Security Considerations

- **Never hardcode credentials** in source code
- **Use environment variables** or **secrets manager** for sensitive data:
  ```csharp
  options.Username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
  options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
  ```
- **Use app-specific passwords** for Gmail (not your main password)
- **Always use TLS/SSL** (port 587 with STARTTLS or port 465 with SSL)
- **Store configuration** in secure configuration providers

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Authentication failed | Wrong credentials | Verify username and password |
| Connection timeout | Server unreachable | Check host, port, and firewall |
| SSL/TLS error | Wrong SSL settings | Use `UseSsl=true` for port 465, `UseStartTls=true` for 587 |
| No recipients | Missing To field | Ensure `message.To` is populated |
| Message validation failed | Missing required fields | Check Subject, Body, and From fields |

## License

DotMailer.SMTP is part of the DotMailer suite and follows the same licensing.
