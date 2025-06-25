namespace AvionRelay.Core.Messages;

/// <summary>
/// Represents the current state of a message in the processing pipeline
/// </summary>
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
