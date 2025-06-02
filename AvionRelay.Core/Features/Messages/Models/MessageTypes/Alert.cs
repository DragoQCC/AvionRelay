using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages.MessageTypes;

/// <summary>
/// Represents a condition or state signaled by the sender. <br/>
/// One-to-one, should be acknowledged, no response required. 
/// </summary>
public abstract record Alert : AvionRelayMessage, IAcknowledge, ISingleReceiver
{
    protected Alert()
    {
        AllowedStates = new List<MessageState>
        {
            MessageState.Created,
            MessageState.Sent,
            MessageState.Received,
            MessageState.Processing,
            MessageState.Failed,
            MessageState.AcknowledgementReceived
        };

        Metadata.BaseMessageType = BaseMessageType.Alert;
    }
}