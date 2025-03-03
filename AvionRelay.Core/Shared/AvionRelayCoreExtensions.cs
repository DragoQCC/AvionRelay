using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.Core;

public static class AvionRelayCoreExtensions
{
    public static IServiceCollection AddAvionRelayCore(this IServiceCollection services)
    {
        services.AddSingleton<IRetryPolicy>();
        return services;
    }
}