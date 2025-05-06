using AvionRelay.Core.Handlers;

namespace AvionRelay.Core.Messages;

/// <summary>
/// Enhanced base class for all message states in the state pattern.
/// Each state can have its own chain of processors.
/// </summary>
public abstract class EnhancedMessageState : MessageState
{
    /// <summary>
    /// Registry for state-specific processors.
    /// This is injected by the message processing system.
    /// </summary>
    protected StateProcessorRegistry ProcessorRegistry { get; private set; }
    
    /// <summary>
    /// Sets the processor registry for this state.
    /// </summary>
    /// <param name="registry">The processor registry</param>
    internal void SetProcessorRegistry(StateProcessorRegistry registry)
    {
        ProcessorRegistry = registry;
    }
    
    /// <summary>
    /// Processes a message using the processors registered for this state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TState">The state type</typeparam>
    /// <param name="message">The message to process</param>
    /// <returns>The processed message</returns>
    public virtual async Task<T> ProcessWithStateProcessors<T, TState>(T message) 
        where T : AvionRelayMessageBase 
        where TState : MessageState
    {
        if (ProcessorRegistry != null)
        {
            return await ProcessorRegistry.ProcessMessage<T, TState>(message, ProgressContext);
        }
        
        return message;
    }
}
