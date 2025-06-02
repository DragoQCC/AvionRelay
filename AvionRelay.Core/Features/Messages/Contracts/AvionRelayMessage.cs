using System.Text.Json.Serialization;

namespace AvionRelay.Core.Messages;

public interface IAvionRelayMessage;


public abstract record AvionRelayMessage : IAvionRelayMessage
{
    public MessageContext Metadata { get;  set; } = new();

    /// <inheritdoc />
    internal List<MessageState> AllowedStates { get; init; }
    
}