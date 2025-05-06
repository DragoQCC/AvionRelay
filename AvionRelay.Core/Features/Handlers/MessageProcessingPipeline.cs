using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Handlers;

/// <summary>
/// A pipeline for processing messages through state-specific processors.
/// </summary>
public class MessageProcessingPipeline
{
    private readonly StateProcessorRegistry _processorRegistry;
    
    public MessageProcessingPipeline(StateProcessorRegistry processorRegistry)
    {
        _processorRegistry = processorRegistry;
    }
    
    /// <summary>
    /// Processes a message using the appropriate processors for its current state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="progress">The message progress containing the current state</param>
    /// <returns>The processed message</returns>
    public async Task<T> ProcessMessage<T>(T message, MessageProgress progress) where T : AvionRelayMessageBase
    {
        if (progress.State == null)
        {
            return message;
        }
        
        // Set the processor registry on the state if it's an EnhancedMessageState
        if (progress.State is EnhancedMessageState enhancedState)
        {
            enhancedState.SetProcessorRegistry(_processorRegistry);
        }
        
        // Use reflection to call the appropriate ProcessMessage method based on the state type
        var stateType = progress.State.GetType();
        var method = typeof(StateProcessorRegistry)
            .GetMethod("ProcessMessage")
            ?.MakeGenericMethod(typeof(T), stateType);
        
        if (method != null)
        {
            return (T)await (Task<T>)method.Invoke(_processorRegistry, [ message, progress ]);
        }
        
        return message;
    }
    
    /// <summary>
    /// Processes a message envelope using the appropriate processors for its current state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope to process</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> ProcessEnvelope<T>(MessageEnvelope<T> envelope) where T : AvionRelayMessageBase
    {
        if (envelope.Progress?.State == null)
        {
            return envelope;
        }
        
        var processedMessage = await ProcessMessage(envelope.Message, envelope.Progress);
        
        // Create a new envelope with the processed message
        return new MessageEnvelope<T>(processedMessage, envelope.Metadata, envelope.Sender, envelope.Receivers)
        {
            Progress = envelope.Progress
        };
    }
}
