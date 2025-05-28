using System.ComponentModel.DataAnnotations;
using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages;



/// <summary>
/// A container for shared metadata, correlation info, 
/// or other pipeline-related data that travels alongside a message.
/// </summary>
public class MessageContext
{
    /// <summary>
    /// The ID for this message instance
    /// </summary>
    public Guid MessageId { get; } = Guid.CreateVersion7();
    
    /// <summary>
    /// The time in UTC that the message instance was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// For Commands, Alerts this list will always contain 1 item. <br/>
    /// For Inspections and Notifications, this list will contain 1 or more items.
    /// </summary>
    public List<Acknowledgement> Acknowledgements { get; } = new();
    
    /// <summary>
    /// The current state of the message used to track its progress
    /// </summary>
    public MessageState State { get; internal set; } = new MessageState.Created();

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
    public int RetryCount { get; internal set; }
    
    /// <summary>
    /// Informs the system of the base type for the message. Ex. Command, Notification, Alert, Inspection
    /// </summary>
    public BaseMessageType BaseMessageType { get; internal set; }
}


public record Acknowledgement
{
    public Guid MessageId { get; init; }
    public MessageReceiver Acknowledger { get; init; }
    public DateTimeOffset AcknowledgedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public Acknowledgement(Guid messageId, MessageReceiver acknowledger)
    {
        MessageId = messageId;
        Acknowledger = acknowledger;
    }
}