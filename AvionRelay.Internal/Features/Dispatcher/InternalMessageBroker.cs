using System.Collections.Concurrent;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Internal;

/// <summary>
/// Broker that manages the communication between internal senders and receivers.
/// Provides a centralized registry for message subscriptions and routing.
/// </summary>
public class InternalMessageBroker
{
    private readonly InternalMessageChannel _messageChannel;
    private readonly ILogger<InternalMessageBroker> _logger;
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _subscriptions = new();
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object>> _responseWaiters = new();
    
    public InternalMessageBroker(InternalMessageChannel messageChannel, ILogger<InternalMessageBroker> logger)
    {
        _messageChannel = messageChannel;
        _logger = logger;
    }
    
    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handler">The handler function</param>
    public void Subscribe<T>(Func<MessageEnvelope<T>, Task> handler) where T : AvionRelayMessageBase
    {
        var messageType = typeof(T);
        
        _subscriptions.AddOrUpdate(
            messageType,
            _ => new List<Func<object, Task>> { CreateHandlerWrapper(handler) },
            (_, handlers) =>
            {
                handlers.Add(CreateHandlerWrapper(handler));
                return handlers;
            });
        
        _logger.LogInformation("Subscribed to messages of type {MessageType}", messageType.Name);
    }
    
    /// <summary>
    /// Creates a wrapper for a typed handler that can be stored in the subscriptions dictionary.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handler">The typed handler</param>
    /// <returns>A wrapper function that can handle objects</returns>
    private Func<object, Task> CreateHandlerWrapper<T>(Func<MessageEnvelope<T>, Task> handler) where T : AvionRelayMessageBase
    {
        return async envelope =>
        {
            if (envelope is MessageEnvelope<T> typedEnvelope)
            {
                await handler(typedEnvelope);
            }
        };
    }
    
    /// <summary>
    /// Publishes a message to all subscribers.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <returns>A task that completes when the message is published</returns>
    public async Task PublishAsync<T>(MessageEnvelope<T> envelope) where T : AvionRelayMessageBase
    {
        var messageType = typeof(T);
        
        _logger.LogInformation("Publishing message {MessageId} of type {MessageType}", envelope.Message.MessageId, messageType.Name);
        
        // Send the message to the channel
        await _messageChannel.SendAsync(envelope);
        
        // If there are subscribers, notify them
        if (_subscriptions.TryGetValue(messageType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    await handler(envelope);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in message handler for message {MessageId}", envelope.Message.MessageId);
                }
            }
        }
    }
    
    public async Task<MessageEnvelopeWrapper?> ReceiveNextMessageAsync()
    {
        return await _messageChannel.ReadNextAsync();
    }
    
    /// <summary>
    /// Sends a message and waits for a response.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <param name="timeout">The maximum time to wait for a response</param>
    /// <returns>The response, or null if the timeout expired</returns>
    public async Task<TResponse?> SendAndWaitForResponseAsync<T, TResponse>(MessageEnvelope<T> envelope, TimeSpan timeout) where T : AvionRelayMessageBase, IRespond<TResponse>
    {
        var messageId = envelope.Message.MessageId;
        var tcs = new TaskCompletionSource<object>();
        
        // Register a waiter for the response
        _responseWaiters[messageId] = tcs;
        
        try
        {
            // Publish the message
            await PublishAsync(envelope);
            
            // Wait for the response
            if (await Task.WhenAny(tcs.Task, Task.Delay(timeout)) == tcs.Task)
            {
                var response = await tcs.Task;
                return (TResponse)response;
            }
            
            _logger.LogWarning("Timeout waiting for response to message {MessageId}", messageId);
            return default;
        }
        finally
        {
            // Clean up the waiter
            _responseWaiters.TryRemove(messageId, out _);
        }
    }
    
    /// <summary>
    /// Responds to a message.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="messageId">The ID of the message being responded to</param>
    /// <param name="response">The response</param>
    public void RespondToMessage<T, TResponse>(Guid messageId, TResponse response) where T : AvionRelayMessageBase, IRespond<TResponse>
    {
        if (_responseWaiters.TryGetValue(messageId, out var tcs))
        {
            tcs.TrySetResult(response);
            _logger.LogInformation("Response sent for message {MessageId}", messageId);
        }
        else
        {
            _logger.LogWarning("No waiter found for message {MessageId}", messageId);
        }
    }
}
