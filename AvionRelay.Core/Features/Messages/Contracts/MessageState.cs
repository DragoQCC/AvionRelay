namespace AvionRelay.Core.Messages;

// I want to use the state pattern here
public abstract class MessageState
{
    protected MessageProgress ProgressContext { get; set; }

    
    internal MessageState(){}

    internal void SetProgress(MessageProgress progressContext)
    {
        ProgressContext = progressContext;
    }

    public abstract Task OnEnter();
    public abstract Task OnExit();

}