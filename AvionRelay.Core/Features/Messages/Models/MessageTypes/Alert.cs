using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages.MessageTypes;

/// <summary>
/// Represents a condition or state signaled by the sender. <br/>
/// One-to-one, should be acknowledged, no response required. 
/// </summary>
public abstract class Alert : AvionRelayMessageBase , IAcknowledge, ISingleReceiver
{
    /// <inheritdoc />
    public MessageReceiver Receiver { get; }
    
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