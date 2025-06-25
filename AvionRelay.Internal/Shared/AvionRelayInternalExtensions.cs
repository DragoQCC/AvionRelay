using AvionRelay.Core.Dispatchers;
using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.Internal;

public static class AvionRelayInternalExtensions
{

    
    /// <summary>
    /// Adds the internal message bus to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection WithInternalMessageDispatcher(this IServiceCollection services)
    {
        // Register the message broker as a singleton
        services.AddSingleton<AvionRelayMessageBus,AvionRelayInternalMessageBus>();
        services.AddSingleton<AvionRelayInternalMessageBus>();
        return services;
    }
}