using System.Text.Json.Serialization;
using HelpfulTypesAndExtensions;

namespace AvionRelay.Core.Messages;

// I want to use the state pattern here
/*public record MessageState : IEnumeration<MessageState>
{
    public static Enumeration<MessageState> States { get; } = new();
    
    /// <inheritdoc />
    public string DisplayName { get; }
    /// <inheritdoc />
    public int Value { get; }
    
    public MessageState(string displayName, int value) => (DisplayName, Value) = (displayName, value);
    

    public static MessageState Created => new MessageState("Created", 0);
    public static MessageState Sent => new MessageState("Sent", 1);
    public static MessageState Received => new MessageState("Received", 2);
    public static MessageState Acknowledged => new MessageState("Acknowledged", 3);
    public static MessageState Processing => new MessageState("Processing", 4);
    public static MessageState Responded => new MessageState("Responded", 5);
}
public record FinalizedMessageState : MessageState
{
    /// <inheritdoc />
    public FinalizedMessageState(string displayName, int value) : base(displayName, value)
    {
    }

    public static FinalizedMessageState Completed => new FinalizedMessageState("Completed", 6);
    public static FinalizedMessageState ResponseReceived => new FinalizedMessageState("ResponseReceived", 7);
    public static FinalizedMessageState Failed => new FinalizedMessageState("Failed", 8);

}
*/


/*public abstract record MessageState
{
    public record Created : MessageState;
    public record Sent : MessageState;
    public record Received : MessageState;
    public record Processing : MessageState;
    public record Responded : MessageState;
}

public abstract record FinalizedMessageState : MessageState
{
    public record ResponseReceived : MessageState;
    public record AcknowledgementReceived : MessageState;
    public record Failed : MessageState;
}*/

/// <summary>
/// Represents the current state of a message in the processing pipeline
/// </summary>
//[JsonConverter(typeof(JsonStringEnumConverter<MessageState>))]
public enum MessageState
{
    /// <summary>
    /// Message has been created but not yet sent
    /// </summary>
    Created = 0,
    
    /// <summary>
    /// Message has been sent to the transport layer
    /// </summary>
    Sent = 1,
    
    /// <summary>
    /// Message has been received by the target system
    /// </summary>
    Received = 2,
    
    /// <summary>
    /// Message is currently being processed
    /// </summary>
    Processing = 3,
    
    /// <summary>
    /// Message has been responded to (for commands/inspections)
    /// </summary>
    Responded = 4,
    
    /// <summary>
    /// Response has been received for this message (final state)
    /// </summary>
    ResponseReceived = 100,
    
    /// <summary>
    /// Acknowledgement has been received for this message (final state)
    /// </summary>
    AcknowledgementReceived = 101,
    
    /// <summary>
    /// Message processing has failed (final state)
    /// </summary>
    Failed = 200
}

/// <summary>
/// Extension methods for MessageState enum
/// </summary>
public static class MessageStateExtensions
{
    /// <summary>
    /// Determines if the state is a final state (no further transitions possible)
    /// </summary>
    public static bool IsFinalState(this MessageState state)
    {
        return state switch
        {
            MessageState.ResponseReceived => true,
            MessageState.AcknowledgementReceived => true,
            MessageState.Failed => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Determines if the state indicates successful completion
    /// </summary>
    public static bool IsSuccessState(this MessageState state)
    {
        return state switch
        {
            MessageState.ResponseReceived => true,
            MessageState.AcknowledgementReceived => true,
            _ => false
        };
    }
    
    /// <summary>
    /// Gets a human-readable description of the state
    /// </summary>
    public static string GetDescription(this MessageState state)
    {
        return state switch
        {
            MessageState.Created => "Message created",
            MessageState.Sent => "Message sent",
            MessageState.Received => "Message received",
            MessageState.Processing => "Message being processed",
            MessageState.Responded => "Message responded to",
            MessageState.ResponseReceived => "Response received",
            MessageState.AcknowledgementReceived => "Acknowledgement received", 
            MessageState.Failed => "Message failed",
            _ => "Unknown state"
        };
    }
}
