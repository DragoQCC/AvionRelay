using AvionRelay.Core.Handlers;
using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

/// <summary>
/// Factory for creating chains of state-specific processors.
/// </summary>
public class StateProcessorChainFactory
{
    private readonly StateProcessorRegistry _registry;
    
    public StateProcessorChainFactory(StateProcessorRegistry registry)
    {
        _registry = registry;
    }
    
    
    /// <summary>
    /// Creates a custom processor chain for a specific state.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <param name="processors">The processors to include in the chain</param>
    public void CreateCustomProcessorChain<TState>(params IStateSpecificProcessor<TState>[] processors) where TState : MessageState
    {
        if (processors.Length == 0)
        {
            return;
        }
        
        // Link the processors together
        for (var i = 0; i < processors.Length - 1; i++)
        {
            processors[i].SetNextStateProcessor(processors[i + 1]);
        }
        
        // Register the first processor in the chain
        _registry.RegisterProcessor(processors[0]);
    }
}
