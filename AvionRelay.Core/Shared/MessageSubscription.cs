using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

public interface IAvionRelayMessageSubscription
{
    public MessageReceiver MessageReceiver { get; }
    public Task HandleAsync(Package wrapper);
    public Task OnSubscribe();
    public Task OnUnsubscribe();
}

public sealed class MessageSubscription<TMessage> : IAvionRelayMessageSubscription  where TMessage : AvionRelayMessage
{
    /// <inheritdoc />
    public MessageReceiver MessageReceiver { get; internal set; }
    private Func<TMessage, Task> _handler;
    private Func<Task>? _onSubscribe;
    private Func<Task>? _onUnsubscribe;
    
    
    internal MessageSubscription(MessageReceiver receiver,Func<TMessage, Task> handler, Func<Task>? onSubscribe = null, Func<Task>? onUnsubscribe = null)
    {
        MessageReceiver = receiver;
        _handler = handler;
        _onSubscribe = onSubscribe;
        _onUnsubscribe = onUnsubscribe;
    }

    

    public async Task HandleAsync(Package wrapper)
    {
        if (wrapper.Message is TMessage message)
        {
            await _handler(message);
        }
    }

    /// <inheritdoc />
    public async Task OnSubscribe()
    {
        if (_onSubscribe is not null)
        {
            await _onSubscribe();
        }
    }

    /// <inheritdoc />
    public async Task OnUnsubscribe()
    {
        if (_onUnsubscribe is not null)
        {
            await _onUnsubscribe();
        }
    }
}