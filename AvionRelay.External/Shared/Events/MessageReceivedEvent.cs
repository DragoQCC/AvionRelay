using AvionRelay.Core.Messages;
using IntercomEventing.Features.Events;

namespace AvionRelay.External;

public record MessageReceivedEvent : GenericEvent<MessageReceivedEvent>
{
    private Package _package;
    private string _fromClientId;
    private int _messageSize;
    
    public async Task NotifyMessageReceived(Package package, string fromClientId, int messageSize)
    {
        _package = package;
        _fromClientId = fromClientId;
        _messageSize = messageSize;
        MessageReceivedEventCall call = CreateEventCall();
        await RaiseEvent(call);
    }
    
    /// <inheritdoc />
    override protected MessageReceivedEventCall CreateEventCall() => new()
    {
        Package = _package,
        FromClientId = _fromClientId,
        MessageSize = _messageSize
    };
}

public record MessageReceivedEventCall : EventCall<MessageReceivedEvent>
{
    public required Package Package { get; init; }
    public required string FromClientId { get; init; }
    public int MessageSize { get; init; }
}