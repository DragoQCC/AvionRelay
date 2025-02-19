namespace AvionRelay.Core.Messages;

/// <summary>
/// A container for shared metadata, correlation info, 
/// or other pipeline-related data that travels alongside a message.
/// </summary>
public class MessageContext
{
    /// <summary>
    /// A correlation ID that ties related messages or operations together.
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Indicates the time at which the context was created/attached.
    /// </summary>
    public DateTime ContextCreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Priority of the message for pipeline decisions, scheduling, etc.
    /// </summary>
    public MessagePriority Priority { get; set; }

    /// <summary>
    /// Optional cancellation token (if you want to support message cancellation).
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    // You can add more fields for things like headers, tracing data, etc.
}
