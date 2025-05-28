using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Processors;

public abstract class MessageProcessor
{
    private MessageProcessor? _nextProcessor;
    
    
    public MessageProcessor SetNextProcessor(MessageProcessor processor)
    {
        _nextProcessor = processor;
        return processor;
    }

    
    public virtual async Task<T> ProcessMessage<T>(T message) where T : AvionRelayMessage
    {
        if (_nextProcessor is not null)
        {
            return await _nextProcessor.ProcessMessage(message);
        }
        return message;
    }
}