using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages.MessageTypes;

/// <summary>
/// Represents a notification broadcast <br/>
/// One-to-many, should be acknowledged, no response required.
/// </summary>
public abstract class Notification : AvionRelayMessageBase, IAcknowledge, IMultiReceiver
{
    /// <inheritdoc />
    public List<MessageReceiver> Receivers { get; }
    
    /// <inheritdoc />
    bool IAcknowledge.IsAcknowledged { get; set; }

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

   
}