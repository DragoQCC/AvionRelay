namespace AvionRelay.Core.Messages;

/// <summary>
/// A final state for the message
/// This final state indicates that the message has been processed successfully and no further action is required.
/// </summary>
public sealed class ResponseReceived : MessageState
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