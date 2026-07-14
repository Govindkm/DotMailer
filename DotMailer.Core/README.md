# Govindkm.DotMailer.Core

Core domain models and interfaces for the DotMailer email delivery system.

## Overview

This package provides the essential types and interfaces for building email delivery applications. It includes:

- **EmailMessage** - The main model for representing emails with support for attachments, CC/BCC, and HTML content
- **EmailAddress** - Type-safe email address representation with optional display names
- **EmailSendResult** - Result object indicating success/failure of email delivery
- **IEmailTransport** - Interface for implementing custom email transport providers

## Installation

```bash
dotnet add package Govindkm.DotMailer.Core
```

## Quick Start

```csharp
using Govindkm.DotMailer.Core;

// Create an email message
var message = new EmailMessage
{
    From = new EmailAddress("sender@example.com", "Sender Name"),
    To = new[] { new EmailAddress("recipient@example.com") },
    Subject = "Hello",
    TextBody = "This is a test email",
    HtmlBody = "<p>This is a test email</p>"
};

// Use with a transport implementation (e.g., Govindkm.DotMailer.SMTP)
var result = await client.SendAsync(message);

if (result.IsSuccess)
{
    Console.WriteLine($"Email sent with ID: {result.MessageId}");
}
else
{
    Console.WriteLine($"Failed to send email: {result.ErrorMessage}");
}
```

## Features

- ✅ Rich email message model with attachments support
- ✅ Support for CC and BCC recipients
- ✅ Type-safe email address handling
- ✅ Extensible transport interface
- ✅ Pure .NET implementation, no external dependencies

## Documentation

For complete documentation, examples, and more details, visit:
[DotMailer GitHub Repository](https://github.com/Govindkm/DotMailer)

## License

MIT License - See LICENSE file in repository for details
