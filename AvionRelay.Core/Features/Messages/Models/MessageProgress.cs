namespace AvionRelay.Core.Messages;

/// <summary>
/// This class tracks the state of the message in the pipeline
/// </summary>
public class MessageProgress
{
    internal MessageState? State { get; set; } = null;

    public MessageProgress(MessageState state)
    {
        this.ChangeStateTo(state);
    }

    public void ChangeStateTo(MessageState state)
    {
        this.State?.OnExit();
        this.State = state;
        this.State.SetContext(this);
        this.State?.OnEnter();
    }
    
}

// An IMessageState Interface, I want to use the state pattern here
public abstract class MessageState
{
    protected MessageProgress ProgressContext { get; set; }

    public MessageState(MessageProgress progressContext)
    {
        ProgressContext = progressContext;
    }

    internal void SetContext(MessageProgress progressContext)
    {
        this.ProgressContext = progressContext;
    }

    public abstract void OnEnter();
    public abstract void OnExit();

}