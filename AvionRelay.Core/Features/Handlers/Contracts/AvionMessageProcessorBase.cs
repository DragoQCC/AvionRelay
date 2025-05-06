using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Handlers;

public abstract class AvionMessageProcessorBase : IAvionMessageProcessor
{
    private IAvionMessageProcessor? _nextProcessor;
    
    
    /// <inheritdoc />
    public IAvionMessageProcessor SetNextProcessor(IAvionMessageProcessor processor)
    {
        _nextProcessor = processor;
        return processor;
    }

    /// <inheritdoc />
    public virtual async Task<T> ProcessMessage<T>(T message) where T : AvionRelayMessageBase
    {
        if (_nextProcessor is not null)
        {
            return await _nextProcessor.ProcessMessage(message);
        }
        return message;
    }
}