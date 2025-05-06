namespace AvionRelay.Core.Messages;

using RetryCount = (int current, int max);

/// <summary>
/// A container for shared metadata, correlation info, 
/// or other pipeline-related data that travels alongside a message.
/// </summary>
public class MessageContext
{
    /// <summary>
    /// A correlation ID that ties related messages or operations together.
    /// </summary>
    public Guid CorrelationId { get; } = Guid.NewGuid();

    /// <summary>
    /// Indicates the time at which the context was created/attached.
    /// </summary>
    public DateTimeOffset ContextCreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Priority of the message for pipeline decisions, scheduling, etc.
    /// </summary>
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;

    /// <summary>
    /// Optional cancellation token (if you want to support message cancellation).
    /// </summary>
    public CancellationToken CancellationToken { get; internal set; }

    /// <summary>
    /// Indicates whether the message processing or related operation has been canceled.
    /// </summary>
    public bool IsCancelled { get; internal set; }

    /// <summary>
    /// Represents the retry count for a message, including the current retry attempt and the maximum allowed retries.
    /// </summary>
    public RetryCount RetryCount { get; set; } = new RetryCount(0, 1);
    
    
    public void SetPriority(MessagePriority priority)
    {
        Priority = priority;
    }
}
