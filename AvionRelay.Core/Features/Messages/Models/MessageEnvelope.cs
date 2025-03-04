using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages;

public class MessageEnvelope<T> where T : AvionRelayMessageBase
{
    public T Message { get; }
    public MessageContext Metadata { get; }
    public MessageSender Sender { get; set; }
    public MessageReceiver Receiver { get; set; }
    public MessageProgress Progress { get; set; }
    
    public MessageEnvelope(T message, MessageContext metadata)
    {
        Message = message;
        Metadata = metadata;
    }
}