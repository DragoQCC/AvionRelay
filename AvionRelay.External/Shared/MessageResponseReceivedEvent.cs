using AvionRelay.Core.Messages;
using IntercomEventing.Features.Events;

namespace AvionRelay.External;

public record MessageResponseReceivedEvent : GenericEvent<MessageResponseReceivedEvent>
{
    private Guid _messageId;
    private List<JsonResponse> responses;
    
    public async Task NotifyResponseReceived(Guid messageId, List<JsonResponse> responses)
    {
        _messageId = messageId;
        this.responses = responses;
        var call = CreateEventCall();
        await RaiseEvent(call);
    }
    
    /// <inheritdoc />
    override protected MessageResponseReceivedEventCall CreateEventCall() => new(_messageId, responses);
}

public record MessageResponseReceivedEventCall(Guid messageId, List<JsonResponse> responses) : EventCall<MessageResponseReceivedEvent>;