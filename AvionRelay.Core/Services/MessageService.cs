using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Handlers;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Core.Services;

/// <summary>
/// High-level service for sending and processing messages.
/// Provides a simplified API for users.
/// </summary>
public class MessageService
{
    private readonly MessageProcessor _messageProcessor;
    private readonly StateProcessorRegistry _processorRegistry;
    private readonly ILogger<MessageService> _logger;
    
    public MessageService(
        MessageProcessor messageProcessor,
        StateProcessorRegistry processorRegistry,
        ILogger<MessageService> logger)
    {
        _messageProcessor = messageProcessor;
        _processorRegistry = processorRegistry;
        _logger = logger;
    }
    
    /// <summary>
    /// Registers a processor for a specific state.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <typeparam name="TProcessor">The processor type</typeparam>
    public void RegisterProcessor<TState, TProcessor>() where TState : MessageState where TProcessor : IStateSpecificProcessor<TState>, new()
    {
        _processorRegistry.RegisterProcessor(new TProcessor());
    }
    
    /// <summary>
    /// Registers a processor instance for a specific state.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <param name="processor">The processor instance</param>
    public void RegisterProcessor<TState>(IStateSpecificProcessor<TState> processor) where TState : MessageState
    {
        _processorRegistry.RegisterProcessor(processor);
    }
    
    /// <summary>
    /// Sends a message.
    /// This method handles creating the message envelope, setting the initial state,
    /// and processing the message through the pipeline.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to send</param>
    /// <param name="sender">The message sender</param>
    /// <param name="receivers">The message receivers</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> SendMessage<T>(T message, MessageSender sender, List<MessageReceiver> receivers) where T : AvionRelayMessageBase
    {
        _logger.LogInformation("Sending message of type {MessageType}", typeof(T).Name);
        
        return await _messageProcessor.SendMessage(message, sender, receivers);
    }
    
    /// <summary>
    /// Sends a message to a single receiver.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to send</param>
    /// <param name="sender">The message sender</param>
    /// <param name="receiver">The message receiver</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> SendMessage<T>(T message, MessageSender sender, MessageReceiver receiver) where T : AvionRelayMessageBase
    {
        return await SendMessage(message, sender, [ receiver ]);
    }
    
    /// <summary>
    /// Receives a message and processes it through the Received state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> ReceiveMessage<T>(MessageEnvelope<T> envelope) where T : AvionRelayMessageBase
    {
        _logger.LogInformation("Receiving message of type {MessageType}", typeof(T).Name);
        
        // Transition to the Received state
        envelope.Progress.ChangeStateTo(new Received());
        
        // Process the message through the Received state
        return await _messageProcessor.ProcessEnvelope(envelope);
    }
    
    /// <summary>
    /// Acknowledges a message and processes it through the Acknowledged state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> AcknowledgeMessage<T>(MessageEnvelope<T> envelope) where T : AvionRelayMessageBase
    {
        _logger.LogInformation("Acknowledging message of type {MessageType}", typeof(T).Name);
        
        // Acknowledge the message if it implements IAcknowledge
        if (envelope.Message is IAcknowledge acknowledgeable)
        {
            acknowledgeable.Acknowledge();
        }
        
        // Transition to the Acknowledged state
        envelope.Progress.ChangeStateTo(new Acknowledged());
        
        // Process the message through the Acknowledged state
        return await _messageProcessor.ProcessEnvelope(envelope);
    }
    
    /// <summary>
    /// Responds to a message and processes it through the Responded state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <param name="response">The response</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> RespondToMessage<T, TResponse>(MessageEnvelope<T> envelope, TResponse response) where T : AvionRelayMessageBase,IRespond<TResponse>
    {
        _logger.LogInformation("Responding to message of type {MessageType}", typeof(T).Name);
        
        // Send the response
        await envelope.Message.Respond(response);
        
        // Transition to the Responded state
        envelope.Progress.ChangeStateTo(new Responded());
        
        // Process the message through the Responded state
        return await _messageProcessor.ProcessEnvelope(envelope);
    }
    
    /// <summary>
    /// Marks a message as having received a response and processes it through the ResponseReceived state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> MarkResponseReceived<T>(MessageEnvelope<T> envelope) where T : AvionRelayMessageBase
    {
        _logger.LogInformation("Marking response received for message of type {MessageType}", typeof(T).Name);
        
        // Transition to the ResponseReceived state
        envelope.Progress.ChangeStateTo(new ResponseReceived());
        
        // Process the message through the ResponseReceived state
        return await _messageProcessor.ProcessEnvelope(envelope);
    }
    
    /// <summary>
    /// Marks a message as failed and processes it through the Failed state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <param name="exception">The exception that caused the failure</param>
    /// <returns>The processed message envelope</returns>
    public async Task<MessageEnvelope<T>> MarkMessageFailed<T>(MessageEnvelope<T> envelope, Exception exception) where T : AvionRelayMessageBase
    {
        _logger.LogError(exception, "Message of type {MessageType} failed", typeof(T).Name);
        
        // Transition to the Failed state
        envelope.Progress.ChangeStateTo(new Failed());
        
        // Process the message through the Failed state
        return await _messageProcessor.ProcessEnvelope(envelope);
    }
}
