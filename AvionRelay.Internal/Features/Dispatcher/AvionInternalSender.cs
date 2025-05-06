using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Internal;

public class AvionInternalSender : AvionRelay.Core.Dispatchers.MessageSender
{
    private readonly InternalMessageBroker _messageBroker;
    private readonly ILogger<AvionInternalSender> _logger;
    
    public AvionInternalSender(InternalMessageBroker messageBroker, ILogger<AvionInternalSender> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public override async Task Send<T>(MessageEnvelope<T> messageEnvelope)
    {
        try
        {
            _logger.LogInformation("Sending message {MessageId} of type {MessageType}", 
                messageEnvelope.Message.MessageId, typeof(T).Name);
            
            // Ensure the message is in the Sent state
            if (!messageEnvelope.Progress.IsInState<Sent>())
            {
                messageEnvelope.Progress.ChangeStateTo(new Sent());
            }
            
            // Send the message through the broker
            await _messageBroker.PublishAsync(messageEnvelope);
            
            _logger.LogDebug("Message {MessageId} sent successfully", messageEnvelope.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message {MessageId}", messageEnvelope.Message.MessageId);
            
            // Mark the message as failed
            messageEnvelope.Progress.ChangeStateTo(new Failed());
            
            // Re-throw the exception
            throw;
        }
    }
    
    /// <summary>
    /// Sends a message with a specific priority.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="messageEnvelope">The message envelope</param>
    /// <param name="priority">The message priority</param>
    /// <returns>A task that completes when the message is sent</returns>
    public async Task SendWithPriority<T>(MessageEnvelope<T> messageEnvelope, MessagePriority priority) where T : AvionRelayMessageBase
    {
        // Set the priority
        messageEnvelope.Metadata.SetPriority(priority);
        
        // Send the message
        await Send(messageEnvelope);
    }
    
    /// <summary>
    /// Sends a message and waits for it to be received.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="messageEnvelope">The message envelope</param>
    /// <param name="timeout">The maximum time to wait for the message to be received</param>
    /// <returns>True if the message was received, false if the timeout expired</returns>
    public async Task<bool> SendAndWaitForReceived<T>(MessageEnvelope<T> messageEnvelope, TimeSpan timeout) where T : AvionRelayMessageBase
    {
        // Create a wrapper for the message
        var wrapper = new MessageEnvelopeWrapper(messageEnvelope);
        
        // Send the message
        await Send(messageEnvelope);
        
        // Wait for the message to be received
        return await wrapper.WaitForReceivedAsync(timeout);
    }
    
    /// <summary>
    /// Sends a message and waits for a response.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="messageEnvelope">The message envelope</param>
    /// <param name="timeout">The maximum time to wait for a response</param>
    /// <returns>The response, or default if the timeout expired</returns>
    public async Task<TResponse?> SendAndWaitForResponseAsync<T, TResponse>(MessageEnvelope<T> messageEnvelope, TimeSpan timeout)  where T : AvionRelayMessageBase, IRespond<TResponse>
    {
        // Ensure the message is in the Sent state
        if (!messageEnvelope.Progress.IsInState<Sent>())
        {
            messageEnvelope.Progress.ChangeStateTo(new Sent());
        }
        
        // Send the message and wait for a response
        return await _messageBroker.SendAndWaitForResponseAsync<T, TResponse>(messageEnvelope, timeout);
    }
}