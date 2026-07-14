using DotMailer.Core;
using DotMailer.SMTP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotMailer.Extensions.DependencyInjection.Examples;

/// <summary>
/// Example usage of DotMailer SMTP transport with dependency injection.
/// </summary>
public static class SmtpTransportExamples
{
    /// <summary>
    /// Example 1: Basic email sending with programmatic configuration.
    /// </summary>
    public static async Task BasicEmailExample()
    {
        var services = new ServiceCollection();
        services.AddLogging(config => config.AddConsole());

        // Configure SMTP transport
        services.AddDotMailer(builder =>
        {
            builder.AddSmtpTransport(options =>
            {
                options.Host = "smtp.gmail.com";
                options.Port = 587;
                options.Username = "your-email@gmail.com";
                options.Password = "your-app-password";
                options.DefaultFromAddress = "your-email@gmail.com";
                options.DefaultFromDisplayName = "Your Name";
                options.UseStartTls = true;
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var emailClient = serviceProvider.GetRequiredService<IEmailClient>();

        // Create and send email
        var message = new EmailMessage
        {
            From = new EmailAddress("sender@example.com", "Sender Name"),
            Subject = "Welcome to DotMailer!",
            HtmlBody = """
                <h1>Welcome!</h1>
                <p>This is your first email sent with DotMailer SMTP transport.</p>
                <p>Best regards,<br/>DotMailer Team</p>
                """,
            TextBody = "Welcome!\n\nThis is your first email sent with DotMailer SMTP transport.\n\nBest regards,\nDotMailer Team"
        };

        message.To.Add(new EmailAddress("recipient@example.com", "Recipient Name"));

        var result = await emailClient.SendAsync(message);

        if (result.IsSuccess)
        {
            Console.WriteLine($"✓ Email sent! Message ID: {result.MessageId}");
        }
        else
        {
            Console.WriteLine($"✗ Failed: {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// Example 2: Email with multiple recipients, CC, BCC, and attachments.
    /// </summary>
    public static async Task AdvancedEmailExample(IEmailClient emailClient)
    {
        var message = new EmailMessage
        {
            From = new EmailAddress("notifications@company.com", "Company Notifications"),
            Subject = "Project Update - Q3 2024",
            HtmlBody = """
                <h2>Q3 2024 Project Status</h2>
                <ul>
                    <li>Feature A: Completed ✓</li>
                    <li>Feature B: In Progress</li>
                    <li>Feature C: Planned</li>
                </ul>
                <p>See attached document for full details.</p>
                """,
            TextBody = "Q3 2024 Project Status\n- Feature A: Completed\n- Feature B: In Progress\n- Feature C: Planned\n\nSee attached document for full details."
        };

        // Multiple recipients
        message.To.Add(new EmailAddress("manager@company.com", "Manager"));
        message.To.Add(new EmailAddress("cto@company.com", "CTO"));

        // CC team members
        message.Cc.Add(new EmailAddress("dev-team@company.com", "Dev Team"));
        message.Cc.Add(new EmailAddress("qa-team@company.com", "QA Team"));

        // BCC archive
        message.Bcc.Add(new EmailAddress("archive@company.com"));

        // Reply-to support
        message.ReplyTo = new EmailAddress("support@company.com", "Support Team");

        // Add attachment
        var pdfContent = await System.IO.File.ReadAllBytesAsync("project-report.pdf");
        message.Attachments.Add(new EmailAttachment("project-report.pdf", pdfContent, "application/pdf"));

        // Custom headers for tracking
        message.Headers["X-Priority"] = "1";
        message.Headers["X-Report-ID"] = "Q3-2024-001";

        // Tags for analytics
        message.Tags["category"] = "status-update";
        message.Tags["quarter"] = "q3";
        message.Tags["year"] = "2024";

        var result = await emailClient.SendAsync(message);

        if (result.IsSuccess)
        {
            Console.WriteLine($"✓ Advanced email sent with ID: {result.MessageId}");
        }
        else
        {
            Console.WriteLine($"✗ Send failed: {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// Example 3: Using with dependency injection in a service.
    /// </summary>
    public class EmailNotificationService
    {
        private readonly IEmailClient _emailClient;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(IEmailClient emailClient, ILogger<EmailNotificationService> logger)
        {
            _emailClient = emailClient;
            _logger = logger;
        }

        public async Task SendWelcomeEmailAsync(string recipientEmail, string recipientName, string userName)
        {
            var message = new EmailMessage
            {
                Subject = $"Welcome, {userName}!",
                HtmlBody = $"""
                    <h1>Welcome {userName}!</h1>
                    <p>Your account has been created successfully.</p>
                    <p><a href="https://app.example.com/start">Click here to get started</a></p>
                    """,
                TextBody = $"Welcome {userName}!\n\nYour account has been created successfully.\n\nClick here to get started: https://app.example.com/start"
            };

            message.To.Add(new EmailAddress(recipientEmail, recipientName));
            message.Tags["type"] = "welcome";
            message.Tags["user"] = userName;

            _logger.LogInformation("Sending welcome email to {Email}", recipientEmail);

            var result = await _emailClient.SendAsync(message);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Welcome email sent to {Email} with ID {MessageId}", recipientEmail, result.MessageId);
            }
            else
            {
                _logger.LogError("Failed to send welcome email to {Email}: {Error}", recipientEmail, result.ErrorMessage);
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var message = new EmailMessage
            {
                Subject = "Password Reset Request",
                HtmlBody = $"""
                    <p>You requested a password reset.</p>
                    <p><a href="{resetLink}">Click here to reset your password</a></p>
                    <p>This link expires in 24 hours.</p>
                    <p>If you didn't request this, please ignore this email.</p>
                    """,
                TextBody = $"You requested a password reset.\n\nClick here to reset your password: {resetLink}\n\nThis link expires in 24 hours.\n\nIf you didn't request this, please ignore this email."
            };

            message.To.Add(new EmailAddress(email));
            message.Tags["type"] = "password-reset";

            var result = await _emailClient.SendAsync(message);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to send password reset email to {Email}: {Error}", email, result.ErrorMessage);
            }
        }
    }

    /// <summary>
    /// Example 4: Error handling and retries.
    /// </summary>
    public static async Task ErrorHandlingExample(IEmailClient emailClient)
    {
        var message = new EmailMessage
        {
            From = new EmailAddress("app@example.com"),
            Subject = "Test Email",
            HtmlBody = "<p>This is a test</p>",
            TextBody = "This is a test"
        };

        message.To.Add(new EmailAddress("user@example.com"));

        // Retry logic
        const int maxRetries = 3;
        var retryCount = 0;
        EmailSendResult? result = null;

        while (retryCount < maxRetries)
        {
            try
            {
                result = await emailClient.SendAsync(message);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"✓ Email sent on attempt {retryCount + 1}");
                    break;
                }

                retryCount++;

                // Check if error is retryable
                if (result.ErrorMessage?.Contains("timeout") == true || result.ErrorMessage?.Contains("connection") == true)
                {
                    Console.WriteLine($"✗ Attempt {retryCount}: {result.ErrorMessage}. Retrying...");
                    await Task.Delay(1000 * retryCount); // Exponential backoff
                }
                else
                {
                    // Non-retryable error
                    Console.WriteLine($"✗ Non-retryable error: {result.ErrorMessage}");
                    break;
                }
            }
            catch (Exception ex)
            {
                retryCount++;
                Console.WriteLine($"✗ Exception on attempt {retryCount}: {ex.Message}");

                if (retryCount >= maxRetries)
                {
                    Console.WriteLine("✗ Max retries exceeded");
                    break;
                }

                await Task.Delay(1000 * retryCount);
            }
        }
    }
}
