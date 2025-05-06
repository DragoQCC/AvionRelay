using AvionRelay.Core.Handlers;
using AvionRelay.Core.Messages;
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
    public static IServiceCollection AddAvionRelayCore(this IServiceCollection services)
    {
        // Register the state transition graph
        services.AddSingleton(StateTransitionGraph.CreateDefaultGraph());
        
        // Register the processor registry
        services.AddSingleton<StateProcessorRegistry>();
        
        // Register the ProcessorChainFactory
        services.AddSingleton<StateProcessorChainFactory>();
        
        // Register the pipeline as a singleton
        services.AddSingleton<MessageProcessingPipeline>();
        
        // Register the message processor
        services.AddSingleton<MessageProcessor>();
        
        // Register the message service
        services.AddSingleton<MessageService>();
        
        return services;
    }
    
    /// <summary>
    /// Adds a state-specific processor to the service collection.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <typeparam name="TProcessor">The processor type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddStateProcessor<TState, TProcessor>(this IServiceCollection services) where TState : MessageState where TProcessor : class, IStateSpecificProcessor<TState>
    {
        // Register the processor
        services.AddTransient<TProcessor>();
        
        // Register a factory that adds the processor to the registry
        services.AddSingleton<IStartupTask>(provider =>
        {
            return new RegisterProcessorStartupTask<TState, TProcessor>(
                provider.GetRequiredService<StateProcessorRegistry>(),
                provider.GetRequiredService<TProcessor>());
        });
        
        return services;
    }

    /// <summary>
    /// Startup task that registers a processor with the registry.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <typeparam name="TProcessor">The processor type</typeparam>
    private class RegisterProcessorStartupTask<TState, TProcessor> : IStartupTask where TState : MessageState where TProcessor : IStateSpecificProcessor<TState>
    {
        private readonly StateProcessorRegistry _registry;
        private readonly TProcessor _processor;
        
        public RegisterProcessorStartupTask(StateProcessorRegistry registry, TProcessor processor)
        {
            _registry = registry;
            _processor = processor;
        }
        
        public void Execute()
        {
            _registry.RegisterProcessor(_processor);
        }
    }
}