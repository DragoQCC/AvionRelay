using System.Collections.Concurrent;

namespace AvionRelay.Core.Messages;

/// <summary>
/// Defines the valid transitions between message states.
/// </summary>
public class StateTransitionGraph
{
    private readonly ConcurrentDictionary<Type, HashSet<Type>> _validTransitions = new();
    
    /// <summary>
    /// Adds a valid transition from one state to another.
    /// </summary>
    /// <typeparam name="TFrom">The source state type</typeparam>
    /// <typeparam name="TTo">The destination state type</typeparam>
    private void AddTransition<TFrom, TTo>() 
        where TFrom : MessageState 
        where TTo : MessageState
    {
        var fromType = typeof(TFrom);
        var toType = typeof(TTo);
        
        _validTransitions.AddOrUpdate(
            fromType,
            _ => [ toType ],
            (_, existingSet) =>
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
    public bool IsValidTransition(MessageState fromState, MessageState toState)
    {
        var fromType = fromState.GetType();
        var toType = toState.GetType();
        
        return _validTransitions.TryGetValue(fromType, out var validDestinations) && 
               validDestinations.Contains(toType);
    }
    
    
    /// <summary>
    /// Creates the default state transition graph for the message pipeline.
    /// </summary>
    /// <returns>A configured state transition graph</returns>
    public static StateTransitionGraph CreateDefaultGraph()
    {
        var graph = new StateTransitionGraph();
        
        // Define the standard message flow
        graph.AddTransition<Created, Sent>();
        graph.AddTransition<Sent, Received>();
        graph.AddTransition<Received, Acknowledged>();
        graph.AddTransition<Acknowledged, Responded>();
        graph.AddTransition<Responded, ResponseReceived>();
        
        // Any state can transition to Failed
        graph.AddTransition<Created, Failed>();
        graph.AddTransition<Sent, Failed>();
        graph.AddTransition<Received, Failed>();
        graph.AddTransition<Acknowledged, Failed>();
        graph.AddTransition<Responded, Failed>();
        
        return graph;
    }
}
