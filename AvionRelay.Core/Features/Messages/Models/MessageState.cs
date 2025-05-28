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


public abstract record MessageState
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
}


