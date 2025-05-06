using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Core.Handlers;

/// <summary>
/// Central processor for messages that handles state transitions and processor execution.
/// </summary>
public class MessageProcessor
{
    private readonly StateProcessorRegistry _processorRegistry;
    private readonly StateTransitionGraph _transitionGraph;
    private readonly ILogger<MessageProcessor> _logger;
    
    public MessageProcessor(
        StateProcessorRegistry processorRegistry,
        StateTransitionGraph transitionGraph,
        ILogger<MessageProcessor> logger)
    {
        _processorRegistry = processorRegistry;
        _transitionGraph = transitionGraph;
        _logger = logger;
    }
    
    /// <summary>
    /// Processes a message envelope through its current state's processors.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope to process</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> ProcessEnvelope<T>(MessageEnvelope<T> envelope) where T : AvionRelayMessageBase
    {
        if (envelope.Progress?.State == null)
        {
            _logger.LogWarning("Cannot process envelope with null state");
            return envelope;
        }
        
        var currentState = envelope.Progress.State;
        var message = envelope.Message;
        
        // Process the message with the current state's processors
        var processedMessage = await ProcessMessageWithState(message, envelope.Progress);
        
        // Create a new envelope with the processed message
        return new MessageEnvelope<T>(
            processedMessage,
            envelope.Metadata,
            envelope.Sender,
            envelope.Receivers)
        {
            Progress = envelope.Progress
        };
    }
    
    /// <summary>
    /// Processes a message with the processors for its current state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="progress">The message progress containing the current state</param>
    /// <returns>The processed message</returns>
    public async Task<T> ProcessMessageWithState<T>(T message, MessageProgress progress) where T : AvionRelayMessageBase
    {
        if (progress.State == null)
        {
            _logger.LogWarning("Cannot process message with null state");
            return message;
        }
        
        // Use reflection to call the appropriate ProcessMessage method based on the state type
        var stateType = progress.State.GetType();
        var method = typeof(StateProcessorRegistry)
            .GetMethod("ProcessMessage")
            ?.MakeGenericMethod(typeof(T), stateType);
        
        if (method != null)
        {
            try
            {
                return (T)await (Task<T>)method.Invoke(_processorRegistry, [ message, progress ]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message in state {State}", stateType.Name);
                
                // Transition to Failed state
                progress.ChangeStateTo(new Failed());
            }
        }
        
        return message;
    }
    
    /// <summary>
    /// Transitions a message to a new state and processes it with that state's processors.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="progress">The message progress containing the current state</param>
    /// <param name="newState">The new state to transition to</param>
    /// <returns>The processed message</returns>
    public async Task<T> TransitionAndProcess<T>(T message, MessageProgress progress, MessageState newState) 
        where T : AvionRelayMessageBase
    {
        if (progress.State == null)
        {
            // If there's no current state, just set the new state
            progress.ChangeStateTo(newState);
            return await ProcessMessageWithState(message, progress);
        }
        
        // Check if the transition is valid
        if (!_transitionGraph.IsValidTransition(progress.State, newState))
        {
            _logger.LogWarning(
                "Invalid state transition from {FromState} to {ToState}",
                progress.State.GetType().Name,
                newState.GetType().Name);
            
            return message;
        }
        
        // Transition to the new state
        progress.ChangeStateTo(newState);
        
        // Process the message with the new state's processors
        return await ProcessMessageWithState(message, progress);
    }
    
    /// <summary>
    /// Sends a message through the entire pipeline, starting from the Created state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to send</param>
    /// <param name="sender">The message sender</param>
    /// <param name="receivers">The message receivers</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> SendMessage<T>(T message, MessageSender sender, List<MessageReceiver> receivers) 
        where T : AvionRelayMessageBase
    {
        // Create a message context
        var context = new MessageContext();
        
        // Create a message envelope with the initial Created state
        var envelope = new MessageEnvelope<T>(message, context, sender, receivers)
        {
            Progress = new MessageProgress(new Created())
        };
        
        // Process the message through the Created state
        envelope = await ProcessEnvelope(envelope);
        
        // Transition to the Sent state and process
        envelope.Progress.ChangeStateTo(new Sent());
        envelope = await ProcessEnvelope(envelope);
        
        // The rest of the state transitions will be handled by the transport layer
        // or by explicit calls to TransitionAndProcess
        
        return envelope;
    }
}
