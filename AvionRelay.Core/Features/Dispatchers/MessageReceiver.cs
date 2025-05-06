using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Dispatchers;

public abstract class MessageReceiver
{
    public abstract Task Receive<T>(MessageEnvelope<T> messageEnvelope) where T : AvionRelayMessageBase;
}