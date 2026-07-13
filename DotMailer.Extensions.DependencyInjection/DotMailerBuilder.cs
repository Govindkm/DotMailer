using Microsoft.Extensions.DependencyInjection;

namespace DotMailer.Extensions.DependencyInjection;

public sealed class DotMailerBuilder
{
    public IServiceCollection Services { get; }

    public DotMailerBuilder(IServiceCollection services)
    {
        Services = services;
    }
}
