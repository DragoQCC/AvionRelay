namespace AvionRelay.Core.Messages;

/// <summary>
/// The message has been created
/// </summary>
public sealed class Created : MessageState
{
    public override Task OnEnter()
    {
        return Task.CompletedTask;
    }

    public override Task OnExit()
    {
        return Task.CompletedTask;
    }
}