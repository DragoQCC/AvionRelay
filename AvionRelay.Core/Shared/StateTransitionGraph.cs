using System.Collections.Concurrent;
using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

/// <summary>
/// Defines the valid transitions between message states using enums
/// </summary>
internal static class StateTransitionGraph
{
    private static readonly ConcurrentDictionary<MessageState, HashSet<MessageState>> _validTransitions = new();
    
    /// <summary>
    /// Adds a valid transition from one state to another
    /// </summary>
    /// <param name="fromState">The source state</param>
    /// <param name="toState">The destination state</param>
    private static void AddTransition(MessageState fromState, MessageState toState)
    {
        _validTransitions.AddOrUpdate(fromState, _ => [toState], (_, existingSet) =>
        {
            existingSet.Add(toState);
            return existingSet;
        });
    }
    
    /// <summary>
    /// Checks if a transition from one state to another is valid
    /// </summary>
    /// <param name="fromState">The source state</param>
    /// <param name="toState">The destination state</param>
    /// <returns>True if the transition is valid, false otherwise</returns>
    internal static bool IsValidTransition(MessageState fromState, MessageState toState)
    {
        return _validTransitions.TryGetValue(fromState, out var validDestinations) && 
               validDestinations.Contains(toState);
    }
    
    /// <summary>
    /// Gets all valid transitions from a given state
    /// </summary>
    /// <param name="fromState">The source state</param>
    /// <returns>Set of valid destination states</returns>
    internal static HashSet<MessageState> GetValidTransitions(MessageState fromState)
    {
        return _validTransitions.TryGetValue(fromState, out var transitions) ? 
               new HashSet<MessageState>(transitions) : 
               new HashSet<MessageState>();
    }
    
    /// <summary>
    /// Creates the default state transition graph for the message pipeline
    /// </summary>
    internal static void CreateDefaultGraph()
    {
        // Clear existing transitions
        _validTransitions.Clear();
        
        // Define the standard message flow
        AddTransition(MessageState.Created, MessageState.Sent);
        AddTransition(MessageState.Sent, MessageState.Received);
        AddTransition(MessageState.Received, MessageState.Processing);
        
        // For non-response messages (alerts, notifications)
        AddTransition(MessageState.Processing, MessageState.AcknowledgementReceived);
        
        // For response messages (commands, inspections)
        AddTransition(MessageState.Processing, MessageState.Responded);
        AddTransition(MessageState.Processing, MessageState.ResponseReceived);
        AddTransition(MessageState.Responded, MessageState.ResponseReceived);
        
        // Any non-final state can transition to Failed
        AddTransition(MessageState.Created, MessageState.Failed);
        AddTransition(MessageState.Sent, MessageState.Failed);
        AddTransition(MessageState.Received, MessageState.Failed);
        AddTransition(MessageState.Processing, MessageState.Failed);
        AddTransition(MessageState.Responded, MessageState.Failed);
        
        // Allow re-processing in some cases (for retry scenarios)
        AddTransition(MessageState.Failed, MessageState.Processing);
        AddTransition(MessageState.Sent, MessageState.Sent); // Allow resending
    }
    
    /// <summary>
    /// Validates that a message state is allowed for the given message type
    /// </summary>
    /// <param name="messageType">The message type</param>
    /// <param name="state">The state to validate</param>
    /// <returns>True if the state is allowed for this message type</returns>
    internal static bool IsStateAllowedForMessageType(Type messageType, MessageState state)
    {
        // You could implement message-type-specific state restrictions here
        // For now, all states are allowed for all message types
        return true;
    }
}