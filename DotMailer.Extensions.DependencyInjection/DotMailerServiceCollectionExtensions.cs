using DotMailer.Core;
using Microsoft.Extensions.DependencyInjection;

namespace DotMailer.Extensions.DependencyInjection;

public static class DotMailerServiceCollectionExtensions
{
    public static IServiceCollection AddDotMailer(
        this IServiceCollection services,
        Action<DotMailerBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new DotMailerBuilder(services);
        configure(builder);

        return services;
    }
}
