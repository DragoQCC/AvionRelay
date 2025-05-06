using System.Collections.Concurrent;
using AvionRelay.Core.Handlers;
using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

/// <summary>
/// Registry for state-specific processors.
/// Manages the processors for each message state.
/// </summary>
public class StateProcessorRegistry
{
    private readonly ConcurrentDictionary<Type, object> _processorChains = new();
    
    /// <summary>
    /// Registers a processor for a specific state.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <param name="processor">The processor to register</param>
    public void RegisterProcessor<TState>(IStateSpecificProcessor<TState> processor) where TState : MessageState
    {
        var stateType = typeof(TState);
        
        _processorChains.AddOrUpdate(stateType,
            // If no chain exists for this state, create one starting with this processor
            processor,
            // If a chain already exists, append this processor to the end of the chain
            (_, existingChain) =>
            {
                var chain = (IStateSpecificProcessor<TState>)existingChain;
                var current = chain;
                
                // Find the end of the chain
                while (current is {} nextProcessor && nextProcessor.GetType().GetMethod("SetNextStateProcessor")?.Invoke(nextProcessor, [null]) != null)
                {
                    current = (IStateSpecificProcessor<TState>)nextProcessor.GetType().GetMethod("SetNextStateProcessor").Invoke(nextProcessor, [ null ]);
                }
                // Append the new processor
                if (current is {} lastProcessor)
                {
                    lastProcessor.GetType().GetMethod("SetNextStateProcessor")?.Invoke(lastProcessor, [processor]);
                }
                return chain;
            });
    }
    
    /// <summary>
    /// Gets the processor chain for a specific state.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <returns>The first processor in the chain for the specified state, or null if none exists</returns>
    public IStateSpecificProcessor<TState>? GetProcessorChain<TState>() where TState : MessageState
    {
        var stateType = typeof(TState);
        
        if (_processorChains.TryGetValue(stateType, out var chain))
        {
            return (IStateSpecificProcessor<TState>)chain;
        }
        return null;
    }
    
    /// <summary>
    /// Processes a message using the appropriate processor chain for its current state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TState">The state type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="progress">The message progress containing the current state</param>
    /// <returns>The processed message</returns>
    public async Task<T> ProcessMessage<T, TState>(T message, MessageProgress progress) where T : AvionRelayMessageBase where TState : MessageState
    {
        var processor = GetProcessorChain<TState>();
        
        if (processor != null && processor.CanProcess(message, progress))
        {
            return await processor.ProcessStateSpecificMessage(message, progress);
        }
        // If no processor is found or none can process the message, return it unchanged
        return message;
    }
}
