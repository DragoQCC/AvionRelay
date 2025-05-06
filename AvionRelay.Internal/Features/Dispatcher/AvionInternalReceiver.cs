using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Internal;

public class AvionInternalReceiver : AvionRelay.Core.Dispatchers.MessageReceiver
{
    private readonly ILogger<AvionInternalReceiver> _logger;
    private readonly InternalMessageBroker _messageBroker;
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private readonly List<Func<object, Task>> _messageHandlers = new();
    private Task? _processingTask;
    
    public AvionInternalReceiver(InternalMessageBroker messageBroker, ILogger<AvionInternalReceiver> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public override async Task Receive<T>(MessageEnvelope<T> messageEnvelope)
    {
        try
        {
            _logger.LogInformation("Receiving message {MessageId} of type {MessageType}", messageEnvelope.Message.MessageId, typeof(T).Name);
            
            // Ensure the message is in the Received state
            if (!messageEnvelope.Progress.IsInState<Received>())
            {
                messageEnvelope.Progress.ChangeStateTo(new Received());
            }
            
            // Process the message with registered handlers
            await ProcessMessageWithHandlers(messageEnvelope);
            
            //TODO: Do I need to invoke the broker here?
            
            _logger.LogDebug("Message {MessageId} received successfully", messageEnvelope.Message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving message {MessageId}", messageEnvelope.Message.MessageId);
            
            // Mark the message as failed
            messageEnvelope.Progress.ChangeStateTo(new Failed());
            
            // Re-throw the exception
            throw;
        }
    }
    
    /// <summary>
    /// Starts listening for messages.
    /// </summary>
    public void StartListening()
    {
        if (_processingTask != null)
        {
            _logger.LogWarning("Already listening for messages");
            return;
        }
        
        _logger.LogInformation("Starting to listen for messages");
        
        _processingTask = Task.Run(async () =>
        {
            try
            {
                while (!_shutdownTokenSource.Token.IsCancellationRequested)
                {
                    MessageEnvelopeWrapper? wrapper = await _messageBroker.ReceiveNextMessageAsync();
                    
                    if (wrapper != null)
                    {
                        try
                        {
                            // Process the message with registered handlers
                            await ProcessMessageWithHandlers(wrapper.Envelope);
                            
                            // Notify that the message has been received
                            wrapper.NotifyReceived();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message");
                        }
                    }
                    else
                    {
                        // No messages available, wait a bit before checking again
                        await Task.Delay(1000, _shutdownTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                _logger.LogInformation("Message listening stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message listening loop");
            }
        });
    }
    
    /// <summary>
    /// Stops listening for messages.
    /// </summary>
    public async Task StopListening()
    {
        if (_processingTask == null)
        {
            _logger.LogWarning("Not listening for messages");
            return;
        }
        
        _logger.LogInformation("Stopping listening for messages");
        
        _shutdownTokenSource.Cancel();
        
        try
        {
            await _processingTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message listening");
        }
        finally
        {
            _processingTask = null;
        }
    }
    
    /// <summary>
    /// Registers a message handler.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handler">The handler function</param>
    public void RegisterHandler<T>(Func<MessageEnvelope<T>, Task> handler) where T : AvionRelayMessageBase
    {
        _messageHandlers.Add(async envelope =>
        {
            if (envelope is MessageEnvelope<T> typedEnvelope)
            {
                await handler(typedEnvelope);
            }
        });
    }
    
    /// <summary>
    /// Processes a message with registered handlers.
    /// </summary>
    /// <param name="envelope">The message envelope</param>
    private async Task ProcessMessageWithHandlers(object envelope)
    {
        foreach (var handler in _messageHandlers)
        {
            try
            {
                await handler(envelope);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message handler");
            }
        }
    }
    
    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="handler">The handler function</param>
    public void Subscribe<T>(Func<MessageEnvelope<T>, Task> handler) where T : AvionRelayMessageBase
    {
        _messageBroker.Subscribe(handler);
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
        _messageBroker.RespondToMessage<T, TResponse>(messageId, response);
    }
}