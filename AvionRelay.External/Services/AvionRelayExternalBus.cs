using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;

namespace AvionRelay.External;

public abstract class AvionRelayExternalBus
{
    public AvionRelayClient? AvionRelayClient { get; set; }
    
    public abstract Task<AvionRelayClient> RegisterClient(string clientName, string clientVersion, List<string> supportedMessageNames, Dictionary<string, object>? metadata = null);
    
    public abstract Task<ResponsePayload<TResponse>> ExecuteCommand<TCommand, TResponse>(TCommand command,string targetHandler, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TCommand : Command<TResponse>;
    
    public abstract IAsyncEnumerable<ResponsePayload<TResponse>> RequestInspection<TInspection, TResponse>(TInspection inspection, List<string> targetHandlers, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TInspection : Inspection<TResponse>;
    
    public abstract Task PublishNotification<TNotification>(TNotification notification, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TNotification : Notification;
    
    public abstract Task SendAlert<TAlert>(TAlert alert, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TAlert : Alert;
    
    
    public abstract Task RespondToMessage<T, TResponse>(Guid messageID, TResponse response, MessageReceiver responder) where T : AvionRelayMessage, IRespond<TResponse>;
    
    public abstract Task AcknowledgeMessage<T>(Guid messageId, MessageReceiver acknowledger) where T : AvionRelayMessage, IAcknowledge;

    public abstract Task StartAsync(CancellationToken cancellationToken = default);
    
    public abstract Task StopAsync();
}