namespace AvionRelay.Core.Messages;

/// <summary>
/// Something went wrong with the message, should detail the error and what the previous state was and expected state
/// </summary>
public sealed class Failed : MessageState
{
    /// <inheritdoc />
    public override async Task OnEnter()
    {
        
    }

    /// <inheritdoc />
    public override async Task OnExit()
    {
        
    }
}