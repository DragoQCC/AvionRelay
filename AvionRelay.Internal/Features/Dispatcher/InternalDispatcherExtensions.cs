using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.Internal;

/// <summary>
/// Extension methods for registering internal dispatcher components with DI.
/// </summary>
public static class InternalDispatcherExtensions
{
    /// <summary>
    /// Adds the internal message dispatcher to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInternalMessageDispatcher(this IServiceCollection services)
    {
        // Register the message channel as a singleton
        services.AddSingleton<InternalMessageChannel>();
        
        // Register the sender and receiver as singletons
        services.AddSingleton<AvionInternalSender>();
        services.AddSingleton<AvionInternalReceiver>();
        
        // Register the message broker as a singleton
        services.AddSingleton<InternalMessageBroker>();
        
        // Register a hosted service to start and stop the receiver
        services.AddHostedService<InternalReceiverHostedService>();
        
        return services;
    }
}

/// <summary>
/// Hosted service that starts and stops the internal receiver.
/// </summary>
public class InternalReceiverHostedService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly AvionInternalReceiver _receiver;
    
    public InternalReceiverHostedService(AvionInternalReceiver receiver)
    {
        _receiver = receiver;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _receiver.StartListening();
        return Task.CompletedTask;
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _receiver.StopListening();
        await base.StopAsync(cancellationToken);
    }
}
