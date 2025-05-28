using System.Collections.Concurrent;
using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

/// <summary>
/// Defines the valid transitions between message states.
/// </summary>
internal static class StateTransitionGraph
{
    private static readonly ConcurrentDictionary<Type, HashSet<Type>> _validTransitions = new();
    
    /// <summary>
    /// Adds a valid transition from one state to another.
    /// </summary>
    /// <typeparam name="TFrom">The source state type</typeparam>
    /// <typeparam name="TTo">The destination state type</typeparam>
    private static void AddTransition<TFrom, TTo>()  where TFrom : MessageState  where TTo : MessageState
    {
        var fromType = typeof(TFrom);
        var toType = typeof(TTo);
        
        _validTransitions.AddOrUpdate(fromType, _ => [ toType ], (_, existingSet) =>
            {
                existingSet.Add(toType);
                return existingSet;
            });
    }
    
    /// <summary>
    /// Checks if a transition from one state to another is valid.
    /// </summary>
    /// <param name="fromState">The source state</param>
    /// <param name="toState">The destination state</param>
    /// <returns>True if the transition is valid, false otherwise</returns>
    internal static bool IsValidTransition(MessageState fromState, MessageState toState)
    {
        var fromType = fromState.GetType();
        var toType = toState.GetType();
        
        return _validTransitions.TryGetValue(fromType, out var validDestinations) && validDestinations.Contains(toType);
    }
    
    
    /// <summary>
    /// Creates the default state transition graph for the message pipeline.
    /// </summary>
    /// <returns>A configured state transition graph</returns>
    internal static void CreateDefaultGraph()
    {
        // Define the standard message flow
        AddTransition<MessageState.Created, MessageState.Sent>();
        AddTransition<MessageState.Sent, MessageState.Received>();
        AddTransition<MessageState.Received, MessageState.Processing>();
        //for non response messages
        AddTransition<MessageState.Processing, FinalizedMessageState.AcknowledgementReceived>();
        
        //for response messages
        AddTransition<MessageState.Processing, MessageState.Responded>();
        AddTransition<MessageState.Processing, FinalizedMessageState.ResponseReceived>();
        AddTransition<MessageState.Responded, FinalizedMessageState.ResponseReceived>();
        
        // Any state can transition to Failed
        AddTransition<MessageState.Created, FinalizedMessageState.Failed>();
        AddTransition<MessageState.Sent, FinalizedMessageState.Failed>();
        AddTransition<MessageState.Received, FinalizedMessageState.Failed>();
        AddTransition<MessageState.Processing, FinalizedMessageState.Failed>();
        AddTransition<MessageState.Responded, FinalizedMessageState.Failed>();
    }
}
