using AvionRelay.Core.Messages;
using IntercomEventing.Features.Events;

namespace AvionRelay.External;

public record MessageResponseReceivedEvent : GenericEvent<MessageResponseReceivedEvent>
{
    private Guid _messageId;
    private List<MessageResponse<object>> responses;
    
    public async Task NotifyResponseReceived(Guid messageId, List<MessageResponse<object>> responses)
    {
        _messageId = messageId;
        this.responses = responses;
        var call = CreateEventCall();
        await RaiseEvent(call);
    }
    
    /// <inheritdoc />
    override protected MessageResponseReceivedEventCall CreateEventCall() => new(_messageId, responses);
}

public record MessageResponseReceivedEventCall(Guid messageId, List<MessageResponse<object>> responses) : EventCall<MessageResponseReceivedEvent>;