using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages.MessageTypes;

/// <summary>
/// Represents a command, one-to-one, where the receiving side performs an action. <br/>
/// Should be acknowledged and should respond.
/// </summary>
public abstract class Command<TResponse> : AvionRelayMessageBase, IRespond<TResponse>, IAcknowledge, ISingleReceiver
{
    /// <inheritdoc />
    bool IAcknowledge.IsAcknowledged { get; set; }
    
    /// <inheritdoc />
    public MessageReceiver Receiver { get; }

    public bool IsAcknowledged
    {
        get => ((IAcknowledge)this).IsAcknowledged;
        internal set => ((IAcknowledge)this).IsAcknowledged = value;
    }

    /// <inheritdoc />
    public void Acknowledge()
    {
        IsAcknowledged = true;
    }
    
    /// <inheritdoc />
    public async Task Respond(TResponse response)
    {
        Console.WriteLine(response);
        await Task.CompletedTask;
    }
}