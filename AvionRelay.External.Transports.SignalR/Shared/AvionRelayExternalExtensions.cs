using AvionRelay.Core.Dispatchers;
using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.External.Transports.SignalR;

public static class AvionRelayExternalExtensions
{
    public static IServiceCollection WithSignalRMessageBus(this IServiceCollection services, Action<SignalRTransportOptions>? configure = null)
    {
        
        var options = new SignalRTransportOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<SignalRTransportClient>();
        services.AddSingleton<AvionRelayMessageBus, AvionRelaySignalRMessageBus>();
        services.AddSingleton<AvionRelaySignalRMessageBus>();
        return services;
    }
    
}