namespace AvionRelay.Core.Messages;

/// <summary>
/// This class tracks the state of the message in the pipeline
/// </summary>
public sealed class MessageProgress
{
    internal MessageState? State { get; set; }

    

    public MessageProgress(MessageState state)
    {
        ChangeStateTo(state);
    }

    public void ChangeStateTo(MessageState state)
    {
        State?.OnExit();
        State = state;
        State.SetProgress(this);
        State?.OnEnter();
    }
    
    public bool IsFinalized => State is ResponseReceived or Failed;
    
    
    public bool IsInState<T>() where T : MessageState => State is T;
}

