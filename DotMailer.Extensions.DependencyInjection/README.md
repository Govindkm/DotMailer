# Govindkm.DotMailer.Extensions.DependencyInjection

Dependency injection extensions for DotMailer. Simplifies registration of email services in ASP.NET Core and .NET applications.

## Overview

This package provides convenient extension methods for the Microsoft.Extensions.DependencyInjection container, allowing you to set up DotMailer with a single method call.

**Features:**
- ✅ One-line service registration
- ✅ Configuration binding support (appsettings.json)
- ✅ Automatic SMTP transport setup
- ✅ Logging integration
- ✅ Type-safe configuration

## Installation

```bash
dotnet add package Govindkm.DotMailer.Extensions.DependencyInjection
```

Also install the core packages:

```bash
dotnet add package Govindkm.DotMailer.Core
dotnet add package Govindkm.DotMailer.SMTP
```

## Quick Start

### Option 1: Configuration from appsettings.json (Recommended)

**appsettings.json:**
```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseSsl": false,
    "UseStartTls": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

**Program.cs:**
```csharp
using Govindkm.DotMailer.Extensions.DependencyInjection;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add DotMailer with automatic configuration from appsettings.json
builder.Services.AddDotMailer();

var app = builder.Build();
app.Run();
```

### Option 2: Programmatic Configuration

```csharp
using Govindkm.DotMailer.Extensions.DependencyInjection;

var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services.AddDotMailer(options =>
{
    options.Host = "smtp.gmail.com";
    options.Port = 587;
    options.UseStartTls = true;
    options.Username = "your-email@gmail.com";
    options.Password = "your-app-password";
});

var app = builder.Build();
```

### Option 3: Configuration Section Name

```csharp
// Use a custom configuration section (default is "SmtpSettings")
builder.Services.AddDotMailer(builder.Configuration, "CustomSmtpSection");
```

## Usage

Once registered, inject `IEmailClient` into your services:

```csharp
using Govindkm.DotMailer.Core;

public class WelcomeEmailService
{
    private readonly IEmailClient _emailClient;

    public WelcomeEmailService(IEmailClient emailClient)
    {
        _emailClient = emailClient;
    }

    public async Task SendWelcomeEmail(string email, string name)
    {
        var message = new EmailMessage
        {
            From = new EmailAddress("noreply@example.com", "Welcome"),
            To = new[] { new EmailAddress(email, name) },
            Subject = "Welcome to our service!",
            HtmlBody = $"<h1>Hello {name}!</h1><p>Thank you for joining us.</p>",
            TextBody = $"Hello {name}!\n\nThank you for joining us."
        };

        var result = await _emailClient.SendAsync(message);
        
        return result.IsSuccess 
            ? $"Welcome email sent (ID: {result.MessageId})"
            : $"Failed to send: {result.ErrorMessage}";
    }
}
```

### In a Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Govindkm.DotMailer.Core;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailClient _emailClient;

    public EmailController(IEmailClient emailClient)
    {
        _emailClient = emailClient;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        var message = new EmailMessage
        {
            From = new EmailAddress("noreply@example.com"),
            Subject = request.Subject,
            HtmlBody = request.Body
        };

        message.To.Add(new EmailAddress(request.To));

        var result = await _emailClient.SendAsync(message);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { messageId = result.MessageId });
    }
}
```

## Configuration Options

The `AddDotMailer()` method accepts these configuration options:

| Option | Type | Required | Default | Description |
|--------|------|----------|---------|-------------|
| `Host` | string | ✅ | — | SMTP server hostname |
| `Port` | int | ❌ | 587 | SMTP server port |
| `Username` | string | ✅ | — | SMTP authentication username |
| `Password` | string | ✅ | — | SMTP authentication password |
| `UseSsl` | bool | ❌ | false | Use SSL/TLS from start (port 465) |
| `UseStartTls` | bool | ❌ | true | Upgrade to TLS (port 587) |
| `TimeoutMs` | int | ❌ | 30000 | Connection timeout in milliseconds |

## Common SMTP Configurations

### Gmail

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseStartTls": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### SendGrid

```json
{
  "SmtpSettings": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "UseStartTls": true,
    "Username": "apikey",
    "Password": "SG.xxxxxxxxxxxxx"
  }
}
```

### AWS SES

```json
{
  "SmtpSettings": {
    "Host": "email-smtp.us-east-1.amazonaws.com",
    "Port": 587,
    "UseStartTls": true,
    "Username": "your-smtp-username",
    "Password": "your-smtp-password"
  }
}
```

## Logging

Enable logging to see DotMailer operations:

```csharp
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);  // See detailed SMTP operations
```

## Error Handling

```csharp
var result = await _emailClient.SendAsync(message);

if (!result.IsSuccess)
{
    _logger.LogError("Email send failed: {Error}", result.ErrorMessage);
    
    // Handle specific errors
    if (result.ErrorMessage.Contains("authentication"))
        return StatusCode(500, "Email service configuration error");
    
    return StatusCode(500, "Failed to send email");
}

_logger.LogInformation("Email sent successfully: {MessageId}", result.MessageId);
```

## Security Best Practices

1. **Never hardcode credentials** - Use configuration files or environment variables
2. **Use secrets manager** in production:
   ```csharp
   builder.Configuration.AddUserSecrets<Program>();
   ```
3. **Use app-specific passwords** for Gmail
4. **Always use TLS/SSL** encryption
5. **Validate email addresses** before sending
6. **Rate limit** to prevent abuse

## Environment Variables

```bash
# Linux/macOS
export SmtpSettings__Username="your-email@gmail.com"
export SmtpSettings__Password="your-app-password"

# Windows (PowerShell)
$env:SmtpSettings__Username="your-email@gmail.com"
$env:SmtpSettings__Password="your-app-password"
```

Then in your code:
```csharp
builder.Configuration.AddEnvironmentVariables();
```

## User Secrets (Development)

```bash
# Initialize user secrets
dotnet user-secrets init

# Set values
dotnet user-secrets set "SmtpSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "SmtpSettings:Password" "your-app-password"

# View all secrets
dotnet user-secrets list
```

## Multiple Email Configurations

If you need multiple email configurations:

```csharp
// Option 1: Register with different configuration sections
services.Configure<SmtpTransportOptions>(
    "Gmail", 
    configuration.GetSection("Smtp:Gmail")
);

services.Configure<SmtpTransportOptions>(
    "SendGrid", 
    configuration.GetSection("Smtp:SendGrid")
);

// Option 2: Use factory pattern to create different clients
services.AddScoped<IGmailEmailClient>(sp => 
    new SmtpEmailClient(sp.GetRequiredService<IOptions<SmtpTransportOptions>>().Value)
);
```

## What Gets Registered

When you call `AddDotMailer()`, the following services are registered:

- `IEmailClient` → `DefaultEmailClient` (Scoped)
- `IEmailTransport` → `SmtpEmailTransport` (Scoped)
- `SmtpTransportOptions` → From configuration (Singleton)
- Logging support for all services

## Troubleshooting

**Issue:** "No 'SmtpSettings' configuration section found"
- **Solution:** Ensure `appsettings.json` has the `SmtpSettings` section, or use `AddDotMailer(config, "CustomSection")`

**Issue:** "Failed to inject IEmailClient"
- **Solution:** Call `AddDotMailer()` before building the service provider

**Issue:** "Authentication failed"
- **Solution:** Verify credentials in configuration, check if app-specific password is needed (Gmail)

**Issue:** "Connection timeout"
- **Solution:** Check Host and Port values, verify firewall allows outbound SMTP connections

## More Information

For complete documentation and examples:
[DotMailer GitHub Repository](https://github.com/Govindkm/DotMailer)

## License

MIT License - See LICENSE file in repository for details
