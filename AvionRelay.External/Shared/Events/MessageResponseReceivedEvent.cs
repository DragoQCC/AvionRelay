using IntercomEventing.Features.Events;

namespace AvionRelay.External;

public record MessageResponseReceivedEvent : GenericEvent<MessageResponseReceivedEvent>
{

    public async Task NotifyResponseReceived(List<ResponsePayload> responses, bool isFinalResponse = false)
    {
        var call = new MessageResponseReceivedEventCall(responses, isFinalResponse);
        await RaiseEvent(call);
    }
    
    /// <inheritdoc />
    override protected MessageResponseReceivedEventCall CreateEventCall() => null;
}

public record MessageResponseReceivedEventCall(List<ResponsePayload> Responses, bool IsFinalResponse = false) : EventCall<MessageResponseReceivedEvent>;