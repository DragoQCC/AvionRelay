using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Handlers;

/// <summary>
/// Interface for processors that are specific to a particular message state.
/// This combines the state pattern with the chain-of-responsibility pattern.
/// </summary>
/// <typeparam name="TState">The specific MessageState type this processor handles</typeparam>
public interface IStateSpecificProcessor<TState> : IAvionMessageProcessor where TState : MessageState
{
    /// <summary>
    /// Determines if this processor can handle the given message in its current state.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="progress">The message progress containing the current state</param>
    /// <returns>True if this processor can handle the message, false otherwise</returns>
    bool CanProcess<T>(T message, MessageProgress progress) where T : AvionRelayMessageBase;

    /// <summary>
    /// Sets the next processor in the chain that handles the same state.
    /// </summary>
    /// <param name="processor">The next processor in the chain</param>
    /// <returns>The processor that was set as the next in the chain</returns>
    public IStateSpecificProcessor<TState> SetNextStateProcessor(IStateSpecificProcessor<TState> processor);
    
    /// <summary>
    /// Processes a message that is in the specific state this processor handles.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message">The message to process</param>
    /// <param name="progress">The message progress containing the current state</param>
    /// <returns>The processed message</returns>
    Task<T> ProcessStateSpecificMessage<T>(T message, MessageProgress progress) where T : AvionRelayMessageBase;
}
