namespace AvionRelay.Core.Messages;

/// <summary>
/// Base interface for all message types within AvionRelay.
/// </summary>
public interface IAvionRelayMessage
{
    /// <summary>
    /// Unique identifier for this message instance.
    /// </summary>
    Guid MessageId { get; }

    /// <summary>
    /// The timestamp when this message was created or dispatched.
    /// </summary>
    DateTime CreatedAt { get; }
}