using AvionRelay.Core.Messages;
using IntercomEventing.Features.Events;

namespace AvionRelay.External;

public record MessageSentEvent : GenericEvent<MessageSentEvent>
{
    private Package _package;
    private string _toClientId;
    private int _messageSize;
    
    public async Task NotifyMessageSent(Package package, string toClientId, int messageSize)
    {
        _package = package;
        _toClientId = toClientId;
        _messageSize = messageSize;
        MessageSentEventCall call = CreateEventCall();
        await RaiseEvent(call);
    }
    
    /// <inheritdoc />
    override protected MessageSentEventCall CreateEventCall(params object[]? args) => new()
    {
        Package = _package,
        ToClientId = _toClientId,
        MessageSize = _messageSize
    };
}

public record MessageSentEventCall : EventCall<MessageSentEvent>
{
    public required Package Package { get; init; }
    public required string ToClientId { get; init; }
    public int MessageSize { get; init; }
}