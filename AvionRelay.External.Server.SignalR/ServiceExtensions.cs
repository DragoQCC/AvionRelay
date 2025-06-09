using AvionRelay.External.Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.External.Server.SignalR;

public static class ServiceExtensions
{
    /// <summary>
    /// Adds SignalR hub support for the SERVER application
    /// </summary>
    public static IServiceCollection AddAvionRelaySignalRHub(this IServiceCollection services, Action<SignalRHubOptions>? configureOptions = null)
    {
        var options = new SignalRHubOptions();
        configureOptions?.Invoke(options);
        
        // Add SignalR
        services.AddSignalR(signalROptions =>
            {
                signalROptions.EnableDetailedErrors = options.EnableDetailedErrors;
                signalROptions.MaximumReceiveMessageSize = options.MaxMessageSize;
                signalROptions.ClientTimeoutInterval = TimeSpan.FromSeconds(options.ClientTimeoutSeconds);
                signalROptions.KeepAliveInterval = TimeSpan.FromSeconds(options.KeepAliveIntervalSeconds);
            }
        );
        
        // Add SignalR monitoring components
        services.AddSingleton<ConnectionTracker>();
        services.AddSingleton<SignalRTransportMonitor>();
        services.AddSingleton<ITransportMonitor>(sp => sp.GetRequiredService<SignalRTransportMonitor>());
        
        return services;
    }
}