using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotMailer.Core;
using DotMailer.SMTP;

namespace DotMailer.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding SMTP transport to DotMailer.
/// </summary>
public static class SmtpDependencyInjectionExtensions
{
    /// <summary>
    /// Adds SMTP transport to the service collection.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddDotMailer(builder =>
    /// {
    ///     builder.AddSmtpTransport(options =>
    ///     {
    ///         options.Host = "smtp.gmail.com";
    ///         options.Port = 587;
    ///         options.Username = "your-email@gmail.com";
    ///         options.Password = "your-app-password";
    ///         options.DefaultFromAddress = "your-email@gmail.com";
    ///         options.DefaultFromDisplayName = "Your Name";
    ///         options.UseStartTls = true;
    ///     });
    /// });
    /// </code>
    /// </example>
    public static DotMailerBuilder AddSmtpTransport(
        this DotMailerBuilder builder,
        Action<SmtpTransportOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SmtpTransportOptions();
        configure(options);
        options.Validate();

        // Register options as singleton
        builder.Services.AddSingleton(options);

        // Register SmtpEmailTransport as IEmailTransport
        builder.Services.AddScoped<IEmailTransport>(provider =>
            new SmtpEmailTransport(
                options,
                provider.GetService<ILogger<SmtpEmailTransport>>()
            )
        );

        // Register a default IEmailClient if not already registered
        builder.Services.AddScoped<IEmailClient>(provider =>
            new DefaultEmailClient(provider.GetRequiredService<IEmailTransport>())
        );

        return builder;
    }

    /// <summary>
    /// Adds SMTP transport with configuration from IConfiguration.
    /// </summary>
    /// <example>
    /// <code>
    /// appsettings.json:
    /// {
    ///   "Smtp": {
    ///     "Host": "smtp.gmail.com",
    ///     "Port": 587,
    ///     "Username": "your-email@gmail.com",
    ///     "Password": "your-app-password",
    ///     "DefaultFromAddress": "your-email@gmail.com",
    ///     "DefaultFromDisplayName": "Your Name"
    ///   }
    /// }
    /// 
    /// Program.cs:
    /// builder.Services.AddDotMailer(b =>
    ///     b.AddSmtpTransport(configuration, "Smtp")
    /// );
    /// </code>
    /// </example>
    public static DotMailerBuilder AddSmtpTransport(
        this DotMailerBuilder builder,
        IConfiguration configuration,
        string sectionName = "Smtp")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
            throw new InvalidOperationException($"Configuration section '{sectionName}' not found");

        return builder.AddSmtpTransport(options =>
            section.Bind(options)
        );
    }
}
