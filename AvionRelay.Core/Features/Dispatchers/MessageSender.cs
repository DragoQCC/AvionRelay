using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Dispatchers;

public abstract class MessageSender
{
    public abstract Task Send<T>(MessageEnvelope<T> messageEnvelope) where T : AvionRelayMessageBase;
}