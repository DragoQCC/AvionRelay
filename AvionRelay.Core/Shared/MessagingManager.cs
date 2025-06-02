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
    
    /// <summary>
    /// Sets the state of a message if the transition is valid
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message to update</param>
    /// <param name="newState">The new state to set</param>
    /// <returns>True if the state was successfully set, false if the transition was invalid</returns>
    public bool SetState<TMessage>(TMessage message, MessageState newState) where TMessage : AvionRelayMessage
    {
        if (message.Metadata.State == newState)
        {
            return true; // Already in the desired state
        }
        
        // Check if the message type allows this state
        if (!IsStateAllowedForMessage(message, newState))
        {
            _logger.LogWarning("State {NewState} is not allowed for message type {MessageType}", 
                               newState, typeof(TMessage).Name);
            return false;
        }
        
        // Check if the transition is valid
        if (StateTransitionGraph.IsValidTransition(message.Metadata.State, newState))
        {
            var oldState = message.Metadata.State;
            message.Metadata.State = newState;
            
            _logger.LogDebug("Message {MessageId} transitioned from {OldState} to {NewState}", 
                             message.Metadata.MessageId, oldState, newState);
            
            return true;
        }
        else
        {
            _logger.LogWarning("Invalid state transition for message {MessageId}: {CurrentState} → {AttemptedState}", 
                               message.Metadata.MessageId, message.Metadata.State, newState);
            return false;
        }
    }
    
    /// <summary>
    /// Checks if a state is allowed for a specific message based on its allowed states
    /// </summary>
    private bool IsStateAllowedForMessage<TMessage>(TMessage message, MessageState state) where TMessage : AvionRelayMessage
    {
        if(message.AllowedStates.Contains(state))
        {
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Acknowledges a message and sets it to the acknowledged state
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message to acknowledge</param>
    /// <param name="ack">The acknowledgement details</param>
    /// <returns>True if the acknowledgement was successful</returns>
    public bool AcknowledgeMessage<TMessage>(TMessage message, Acknowledgement ack) where TMessage : AvionRelayMessage
    {
        message.Metadata.Acknowledgements.Add(ack);
        return SetState(message, MessageState.AcknowledgementReceived);
    }
    
    /// <summary>
    /// Marks a message as failed
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message that failed</param>
    /// <param name="reason">The reason for failure</param>
    /// <returns>True if the state was successfully updated</returns>
    public bool MarkMessageFailed<TMessage>(TMessage message, string reason = "") where TMessage : AvionRelayMessage
    {
        if (!string.IsNullOrEmpty(reason))
        {
            _logger.LogError("Message {MessageId} failed: {Reason}", message.Metadata.MessageId, reason);
        }
        
        return SetState(message, MessageState.Failed);
    }
    
    /// <summary>
    /// Gets all valid next states for a message in its current state
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message</param>
    /// <returns>Set of valid next states</returns>
    public HashSet<MessageState> GetValidNextStates<TMessage>(TMessage message) where TMessage : AvionRelayMessage
    {
        return StateTransitionGraph.GetValidTransitions(message.Metadata.State);
    }
    
    /// <summary>
    /// Checks if a message is in a final state
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="message">The message to check</param>
    /// <returns>True if the message is in a final state</returns>
    public bool IsMessageComplete<TMessage>(TMessage message) where TMessage : AvionRelayMessage
    {
        return message.Metadata.State.IsFinalState();
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