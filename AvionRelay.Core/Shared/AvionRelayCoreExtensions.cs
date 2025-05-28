using AvionRelay.Core.Aspects;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Processors;
using AvionRelay.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.Core;

public static partial class AvionRelayCoreExtensions
{
    /// <summary>
    /// Adds the message processing system to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAvionRelayCore(this IServiceCollection services, Action<AvionRelayOptions>? configure = null)
    {
        // Register the options
        var options = new AvionRelayOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        
        //Create the StateTransitionGraph
        StateTransitionGraph.CreateDefaultGraph();
        
        services.AddSingleton<MessagingManager>();
        
        return services;
    }

    /// <summary>
    /// Adds the in-memory message storage to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns></returns>
    public static IServiceCollection WithInMemoryStorage(this IServiceCollection services)
    {
        services.AddSingleton<IMessageStorage, InMemoryStorage>();
        return services;
    }

}