namespace AvionRelay.Core.Messages;

/// <summary>
/// The message has been acknowledged by the receiver / subscriber
/// </summary>
public sealed class Acknowledged : MessageState
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