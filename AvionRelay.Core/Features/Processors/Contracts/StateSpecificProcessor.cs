using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Processors;

/// <summary>
/// Base class for processors that are specific to a particular message state.
/// Implements the chain-of-responsibility pattern for state-specific processing.
/// </summary>
/// <typeparam name="TState">The specific MessageState type this processor handles</typeparam>
public abstract class StateSpecificProcessor<TState> : MessageProcessor where TState : Enum
{
    private StateSpecificProcessor<TState>? _nextStateProcessor;
    
    /// <summary>
    /// Sets the next processor in the chain that handles the same state.
    /// </summary>
    /// <param name="processor">The next processor in the chain</param>
    /// <returns>The processor that was set as the next in the chain</returns>
    public StateSpecificProcessor<TState> SetNextStateProcessor(StateSpecificProcessor<TState> processor)
    {
        _nextStateProcessor = processor;
        return processor;
    }
    
    // By default, a processor can process a message if it's in the correct state
    public virtual bool CanProcess<T>(T message) where T : AvionRelayMessage =>
        message.Metadata.State is TState;


    public virtual async Task<T> ProcessMessage<T>(T message) where T : AvionRelayMessage
    {
        // Process the message with this processor
        var processedMessage = await ProcessStateMessage(message);
        
        // If there's a next processor in the state-specific chain, pass the message to it
        if (_nextStateProcessor != null && _nextStateProcessor.CanProcess(processedMessage))
        {
            return await _nextStateProcessor.ProcessMessage(processedMessage);
        }
        
        // Otherwise, return the processed message
        return processedMessage;
    }
    
    /// <summary>
    /// The actual processing logic for this specific processor.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="lifecycle">The message progress containing the current state</param>
    /// <returns>The processed message</returns>
    protected abstract Task<T> ProcessStateMessage<T>(T message) where T : AvionRelayMessage;
    
}
