using AvionRelay.Core.Handlers;

namespace AvionRelay.Core.Messages;

/// <summary>
/// This class tracks the state of the message in the pipeline
/// </summary>
public sealed class MessageProgress
{
    private readonly MessageProcessingPipeline _pipeline;
    internal MessageState State { get; private set; } = new Created();
    
    
    public MessageProgress(MessageState state)
    {
        ChangeStateTo(state);
    }
    
    /// <summary>
    /// Creates a new instance of EnhancedMessageProgress.
    /// </summary>
    /// <param name="state">The initial state</param>
    /// <param name="pipeline">The message processing pipeline</param>
    public MessageProgress(MessageState state, MessageProcessingPipeline pipeline)
    {
        ChangeStateTo(state);
        _pipeline = pipeline;
    }

    public void ChangeStateTo(MessageState state)
    {
        State?.OnExit();
        State = state;
        State.SetProgress(this);
        State?.OnEnter();
    }
    
    
    /// <summary>
    /// Changes the state of the message and processes it with the new state's processors.
    /// </summary>
    /// <param name="state">The new state</param>
    /// <param name="message">The message to process</param>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns>The processed message</returns>
    public async Task<T> ChangeStateAndProcess<T>(MessageState state, T message) where T : AvionRelayMessageBase
    {
        // Change the state
        ChangeStateTo(state);
        
        // Process the message with the new state's processors
        return await _pipeline.ProcessMessage(message, this);
    }
    
    /// <summary>
    /// Processes a message with the current state's processors.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <typeparam name="T">The message type</typeparam>
    /// <returns>The processed message</returns>
    public async Task<T> ProcessWithCurrentState<T>(T message) where T : AvionRelayMessageBase
    {
        return await _pipeline.ProcessMessage(message, this);
    }
    
    public MessageState GetState() => State; 
    
    public bool IsFinalized => State is ResponseReceived or Failed;
    
    public bool IsInState<T>() where T : MessageState => State is T;
}

