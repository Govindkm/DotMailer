using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotMailer.Core;
using DotMailer.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add logging with debug level
builder.Logging
    .AddConsole()
    .SetMinimumLevel(LogLevel.Debug);

// Add DotMailer with SMTP transport
builder.Services.AddDotMailer(b =>
{
    b.AddSmtpTransport(builder.Configuration, "SmtpSettings");
});

// Add services
builder.Services.AddScoped<EmailTestService>();

var app = builder.Build();

// Configure middleware
app.UseHttpsRedirection();

// Test endpoints  
app.MapPost("/api/email/test", SendTestEmailAsync)
    .WithName("SendTestEmail");

app.MapPost("/api/email/send", SendEmailAsync)
    .WithName("SendEmail");

app.MapGet("/health", HealthAsync)
    .WithName("Health");

await app.RunAsync();

// Endpoint handlers
async Task<IResult> SendTestEmailAsync(EmailTestService emailService)
{
    var result = await emailService.SendTestEmailAsync();
    return Results.Ok(result);
}

async Task<IResult> SendEmailAsync(SendEmailRequest request, IEmailClient emailClient)
{
    var message = new EmailMessage
    {
        From = new EmailAddress(request.From, request.FromName),
        Subject = request.Subject,
        HtmlBody = request.HtmlBody,
        TextBody = request.TextBody
    };

    foreach (var to in request.To)
    {
        message.To.Add(new EmailAddress(to));
    }

    var result = await emailClient.SendAsync(message);

    if (result.IsSuccess)
    {
        return Results.Ok(new { message = "Email sent successfully", messageId = result.MessageId });
    }
    else
    {
        return Results.BadRequest(new { error = result.ErrorMessage });
    }
}

async Task<IResult> HealthAsync()
{
    return Results.Ok(new { status = "healthy" });
}

// Request model
public record SendEmailRequest(
    string From,
    string? FromName,
    string[] To,
    string Subject,
    string HtmlBody,
    string? TextBody
);

// Email service for testing
public class EmailTestService
{
    private readonly IEmailClient _emailClient;
    private readonly ILogger<EmailTestService> _logger;
    private readonly IConfiguration _configuration;

    public EmailTestService(
        IEmailClient emailClient,
        ILogger<EmailTestService> logger,
        IConfiguration configuration)
    {
        _emailClient = emailClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<object> SendTestEmailAsync()
    {
        var testEmail = _configuration["TestEmail:RecipientEmail"] ?? "test@example.com";
        var fromEmail = _configuration["SmtpSettings:DefaultFromAddress"] ?? "noreply@example.com";

        var message = new EmailMessage
        {
            Subject = "🎉 DotMailer Test Email",
            HtmlBody = """
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h1 style="color: #333;">Welcome to DotMailer! 📧</h1>
                    <p>This is a test email sent via the DotMailer SMTP transport.</p>
                    
                    <h2>What's working:</h2>
                    <ul>
                        <li>✅ SMTP Connection</li>
                        <li>✅ Email Authentication</li>
                        <li>✅ Message Delivery</li>
                        <li>✅ HTML Formatting</li>
                    </ul>
                    
                    <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    
                    <hr style="border: none; border-top: 1px solid #eee; margin: 20px 0;">
                    <p style="font-size: 12px; color: #666;">
                        This is an automated test message. Please do not reply.
                    </p>
                </div>
                """,
            TextBody = $"""
                Welcome to DotMailer!
                
                This is a test email sent via the DotMailer SMTP transport.
                
                What's working:
                - SMTP Connection
                - Email Authentication
                - Message Delivery
                - HTML Formatting
                
                Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                
                This is an automated test message. Please do not reply.
                """
        };

        message.To.Add(new EmailAddress(testEmail, "Test Recipient"));
        message.Tags["type"] = "test";
        message.Tags["service"] = "dotmailer";

        _logger.LogInformation("Sending test email to {Email}", testEmail);

        var result = await _emailClient.SendAsync(message);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Test email sent successfully with ID {MessageId}", result.MessageId);
            return new
            {
                success = true,
                message = "Test email sent successfully!",
                messageId = result.MessageId,
                sentTo = testEmail,
                timestamp = DateTime.UtcNow
            };
        }
        else
        {
            _logger.LogError("Failed to send test email: {Error}", result.ErrorMessage);
            return new
            {
                success = false,
                error = result.ErrorMessage,
                timestamp = DateTime.UtcNow
            };
        }
    }
}