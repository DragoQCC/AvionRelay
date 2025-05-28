using System.Collections.Concurrent;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Core;

public class MessagingManager
{
    private readonly ILogger<MessagingManager> _logger;
    /// <summary>
    /// All message receivers the system has ever seen sorted by their unique IDs
    /// </summary>
    private ConcurrentDictionary<Guid,MessageReceiver> _messageReceivers = new();
    
    /// <summary>
    /// Receivers that are specific to a message type
    /// </summary>
    private ConcurrentDictionary<Type, List<MessageReceiver>> _specificMessageReceivers = new();
    
    private ConcurrentDictionary<Type,HashSet<IAvionRelayMessageSubscription>> _subscriptions = new();
    
    public MessagingManager(ILogger<MessagingManager> logger)
    {
        _logger = logger;
    }
    
    public void SetState<TMessage>(TMessage message, MessageState state) where TMessage : AvionRelayMessage
    {
        if (message.Metadata.State == state)
        {
            return;
        }
        if (message.AllowedStates.Contains(state) && StateTransitionGraph.IsValidTransition(message.Metadata.State, state))
        {
            message.Metadata.State = state;
        }
        else
        {
            _logger.LogInvalidMessageState(message.Metadata.MessageId, message.Metadata.State.ToString(), state.ToString());
        }
    }
    
    public void AcknowledgeMessage<TMessage>(TMessage message, Acknowledgement ack) where TMessage : AvionRelayMessage
    {
        message.Metadata.Acknowledgements.Add(ack);
        SetState(message, new FinalizedMessageState.AcknowledgementReceived());
    }
    
    
    public bool TryRegisterReceiver(MessageReceiver receiver) 
    {
        try
        {
          bool added = _messageReceivers.TryAdd(Guid.Parse(receiver.ReceiverId), receiver);
          if (added)
          {
              _logger.LogInformation("Registered receiver {ReceiverId}", receiver.ReceiverId);
              return added;
          }
          _logger.LogWarning("Receiver {ReceiverId} already registered", receiver.ReceiverId);
          return false;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error registering receiver {ReceiverId}, exception: {Exception}", receiver.ReceiverId, e);
            return false;
        }
    }
    
    public bool TryGetReceiver(Guid receiverId, out MessageReceiver receiver)
    {
        return _messageReceivers.TryGetValue(receiverId, out receiver);
    }
}