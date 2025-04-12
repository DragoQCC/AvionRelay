using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages.MessageTypes;

/// <summary>
/// Represents a request from one sender to multiple receivers. (e.g., a status check or data request) <br/>
/// One-to-many, should be acknowledged, should respond.
/// </summary>
public abstract class Inspection<TResponse> : AvionRelayMessageBase, IRespond<TResponse>, IAcknowledge, IMultiReceiver
{
    /// <inheritdoc />
    bool IAcknowledge.IsAcknowledged { get; set; }

    /// <inheritdoc />
    public List<MessageReceiver> Receivers { get; }
    
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
    }
    
}