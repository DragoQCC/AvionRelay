using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;

namespace AvionRelay.Core.Dispatchers;

public abstract class AvionRelayMessageBus
{
    public abstract Task RegisterMessenger(List<string> supportedMessageNames);
    
    #region Message Sending
    
    public abstract Task<MessageResponse<TResponse>> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TCommand : Command<TResponse>;
    
    public abstract Task<List<MessageResponse<TResponse>>> RequestInspection<TInspection, TResponse>(TInspection inspection, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TInspection : Inspection<TResponse>;
    
    public abstract Task PublishNotification<TNotification>(TNotification notification, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TNotification : Notification;
    
    public abstract Task SendAlert<TAlert>(TAlert alert, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TAlert : Alert;
    
    #endregion
    
    #region Message Responding
    
    public abstract Task RespondToMessage<T, TResponse>(Guid messageId, TResponse response,MessageReceiver responder) where T : AvionRelayMessage, IRespond<TResponse>;
    
    public abstract Task AcknowledgeMessage<T>(Guid messageId, MessageReceiver acknowledger) where T : AvionRelayMessage, IAcknowledge;
    
    #endregion
    
    #region Message Reading
    
    public abstract Task<Package?> ReadNextOutboundMessage(CancellationToken cancellationToken = default);
    
    #endregion
    
    
}
