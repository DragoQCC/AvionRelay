using System.Collections.Concurrent;
using System.Threading.Channels;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Internal;

/// <summary>
/// Provides a channel-based communication mechanism for internal message passing.
/// Uses a priority queue to ensure higher priority messages are processed first.
/// </summary>
public class InternalMessageChannel
{
    private readonly ConcurrentDictionary<MessagePriority, Channel<MessageEnvelopeWrapper>> _priorityChannels = new();
    private readonly ILogger<InternalMessageChannel> _logger;
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    
    public InternalMessageChannel(ILogger<InternalMessageChannel> logger)
    {
        _logger = logger;
        
        // Initialize channels for each priority level
        foreach (MessagePriority priority in Enum.GetValues(typeof(MessagePriority)))
        {
            var channel = Channel.CreateUnbounded<MessageEnvelopeWrapper>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
            
            _priorityChannels[priority] = channel;
        }
        
        // Start the background task that processes messages from the channels
        _ = ProcessMessagesAsync(_shutdownTokenSource.Token);
    }
    
    /// <summary>
    /// Sends a message to the appropriate priority channel.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="envelope">The message envelope</param>
    /// <returns>A task that completes when the message is written to the channel</returns>
    public async Task SendAsync<T>(MessageEnvelope<T> envelope) where T : AvionRelayMessageBase
    {
        var priority = envelope.Metadata.Priority;
        var wrapper = new MessageEnvelopeWrapper(envelope);
        
        if (_priorityChannels.TryGetValue(priority, out var channel))
        {
            await channel.Writer.WriteAsync(wrapper);
            _logger.LogDebug("Message {MessageId} with priority {Priority} sent to channel", envelope.Message.MessageId, priority);
        }
        else
        {
            _logger.LogWarning("No channel found for priority {Priority}", priority);
        }
    }
    
    /// <summary>
    /// Reads the next available message from the highest priority channel that has messages.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read operation</param>
    /// <returns>The next message envelope wrapper, or null if no messages are available</returns>
    public async Task<MessageEnvelopeWrapper?> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        // Start with the highest priority and work down
        foreach (MessagePriority priority in Enum.GetValues(typeof(MessagePriority)).Cast<MessagePriority>().OrderByDescending(p => p))
        {
            if (_priorityChannels.TryGetValue(priority, out var channel))
            {
                if (channel.Reader.Count > 0 && await channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    if (channel.Reader.TryRead(out var wrapper))
                    {
                        _logger.LogDebug("Read message with priority {Priority} from channel", priority);
                        return wrapper;
                    }
                }
            }
        }
        // If we get here, there are no messages available in any channel
        return null;
    }
    
    /// <summary>
    /// Processes messages from the channels in priority order.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the processing</param>
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var wrapper = await ReadNextAsync(cancellationToken);
                
                if (wrapper != null)
                {
                    try
                    {
                        // Notify any subscribers that a message is available
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
                    await Task.Delay(10, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
            _logger.LogInformation("Message processing stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message processing loop");
        }
    }
    
    /// <summary>
    /// Shuts down the message channel.
    /// </summary>
    public void Shutdown()
    {
        _shutdownTokenSource.Cancel();
    }
}

/// <summary>
/// Wraps a message envelope to provide additional functionality.
/// </summary>
public class MessageEnvelopeWrapper
{
    private readonly TaskCompletionSource<bool> _receivedTcs = new();
    private readonly object _envelopeLock = new();
    
    public object Envelope { get; }
    
    public MessageEnvelopeWrapper(object envelope)
    {
        Envelope = envelope;
    }
    
    /// <summary>
    /// Gets the message envelope as a specific type.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns>The message envelope</returns>
    public MessageEnvelope<T> GetEnvelope<T>() where T : AvionRelayMessageBase
    {
        return (MessageEnvelope<T>)Envelope;
    }
    
    /// <summary>
    /// Notifies that the message has been received.
    /// </summary>
    public void NotifyReceived()
    {
        _receivedTcs.TrySetResult(true);
    }
    
    /// <summary>
    /// Waits for the message to be received.
    /// </summary>
    /// <param name="timeout">The maximum time to wait</param>
    /// <returns>True if the message was received, false if the timeout expired</returns>
    public async Task<bool> WaitForReceivedAsync(TimeSpan timeout)
    {
        return await Task.WhenAny(_receivedTcs.Task, Task.Delay(timeout)) == _receivedTcs.Task;
    }
}
