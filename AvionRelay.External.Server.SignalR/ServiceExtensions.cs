using AvionRelay.External.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AvionRelay.External.Server.SignalR;

public static class ServiceExtensions
{
    /// <summary>
    /// Adds SignalR hub support for the SERVER application
    /// </summary>
    public static IServiceCollection AddAvionRelaySignalRHub(this IServiceCollection services, SignalROptions? options = null)
    {
        options ??= new();
        
        // Add SignalR
        services.AddSignalR(signalROptions =>
            {
                signalROptions.EnableDetailedErrors = options.EnableDetailedErrors;
                signalROptions.MaximumReceiveMessageSize = options.MaxMessageSize;
                signalROptions.ClientTimeoutInterval = TimeSpan.FromSeconds(options.ClientTimeoutSeconds);
                signalROptions.KeepAliveInterval = TimeSpan.FromSeconds(options.KeepAliveIntervalSeconds);
                signalROptions.MaximumParallelInvocationsPerClient = 10;
            }
        );
        
        // Add SignalR monitoring components
        services.AddSingleton<ConnectionTracker>();
        services.AddSingleton<SignalRTransportMonitor>();
        services.AddSingleton<ITransportMonitor>(sp => sp.GetRequiredService<SignalRTransportMonitor>());
        //the adapter is required because trying to store a SignalR which is transient directly causes disposed object exceptions
        services.AddSingleton<SignalRTransportAdapter>();
        services.AddHostedService<SignalRTransportRegistration>();
        
        return services;
    }

    public static IEndpointRouteBuilder MapAvionRelaySignalRService(this IEndpointRouteBuilder endpoints, AvionRelayExternalOptions avionRelayOptions)
    {
        if (avionRelayOptions.EnabledTransports.Contains(TransportTypes.SignalR))
        {
            endpoints.MapHub<AvionRelaySignalRTransport>(avionRelayOptions.Transports.SignalR.HubPath, options =>
            {
                options.AllowStatefulReconnects = true;
            });
        }
        return endpoints;
    }
    
    
    // Background service to register transports with the router after DI is complete
    public class SignalRTransportRegistration : IHostedService
    {
        private readonly AvionRelayTransportRouter _router;
        private readonly SignalRTransportAdapter _transport;
    
        public SignalRTransportRegistration(AvionRelayTransportRouter router,SignalRTransportAdapter transport)
        {
            _router = router;
            _transport = transport;
        }
    
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _router.RegisterTransport(_transport);
            return Task.CompletedTask;
        }
    
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _router.UnregisterTransport(_transport.TransportType);
            return Task.CompletedTask;
        }
    }
}