using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Handlers;

public interface IAvionMessageProcessor
{
    IAvionMessageProcessor SetNextProcessor(IAvionMessageProcessor processor);
    
    Task<T> ProcessMessage<T>(T message) where T : AvionRelayMessageBase;
}