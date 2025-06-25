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
    public Guid MessageId { get; set; } = Guid.CreateVersion7();
    
    /// <summary>
    /// The time in UTC that the message instance was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// For Commands, Alerts this list will always contain 1 item. <br/>
    /// For Inspections and Notifications, this list will contain 1 or more items.
    /// </summary>
    public List<Acknowledgement> Acknowledgements { get; set; } = new();
    
    /// <summary>
    /// The current state of the message used to track its progress
    /// </summary>
    public MessageState State { get; set; } = MessageState.Created;

    /// <summary>
    /// Priority of the message for pipeline decisions, scheduling, etc.
    /// </summary>
    public MessagePriority Priority { get; set; } = MessagePriority.Normal;

    /// <summary>
    /// Indicates whether the message processing or related operation has been canceled.
    /// </summary>
    public bool IsCancelled { get;  set; }

    /// <summary>
    /// Represents the current retry attempt for this message <br/>
    /// Should start at 0 and increment on each failure
    /// </summary>
    public int RetryCount { get;  set; }
    
    /// <summary>
    /// Informs the system of the base type for the message. Ex. Command, Notification, Alert, Inspection
    /// </summary>
    public BaseMessageType BaseMessageType { get;  set; }

    public string MessageTypeName { get; set; } = "";
    
    /// <summary>
    /// Optional cancellation token (if you want to support message cancellation).
    /// </summary>
    internal CancellationToken CancellationToken { get; set; }
}


public record Acknowledgement
{
    public Guid MessageId { get; set; }
    public MessageReceiver Acknowledger { get; set; }
    public DateTimeOffset AcknowledgedAt { get; set; }
    
    public Acknowledgement(Guid messageId, MessageReceiver acknowledger, DateTime handledAt)
    {
        MessageId = messageId;
        Acknowledger = acknowledger;
        AcknowledgedAt = handledAt;
    }
    
    public Acknowledgement(Guid messageId, MessageReceiver acknowledger)
    {
        MessageId = messageId;
        Acknowledger = acknowledger;
        AcknowledgedAt = DateTimeOffset.UtcNow;
    }
}