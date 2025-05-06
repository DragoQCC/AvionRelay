using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages;

public class MessageEnvelope<T> where T : AvionRelayMessageBase
{
    public T Message { get; }
    public MessageContext Metadata { get; }
    public MessageSender Sender { get; }
    public List<MessageReceiver> Receivers { get; }
    public MessageProgress Progress { get; set; }
    
    public MessageEnvelope(T message, MessageContext metadata, MessageSender sender, List<MessageReceiver> receivers)
    {
        Message = message;
        Metadata = metadata;
        Sender = sender;
        Receivers = receivers;
    }
    
    public MessageEnvelope(T message, MessageContext metadata, MessageSender sender, MessageReceiver receiver)
    {
        Message = message;
        Metadata = metadata;
        Sender = sender;
        Receivers = [ receiver ];
    }
}