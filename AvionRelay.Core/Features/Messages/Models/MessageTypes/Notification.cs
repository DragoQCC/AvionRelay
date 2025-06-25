namespace AvionRelay.Core.Messages.MessageTypes;

/// <summary>
/// Represents a notification broadcast <br/>
/// One-to-many, does not need to be acknowledged, no response required.
/// </summary>
public abstract class Notification : AvionRelayMessage, IMultiReceiver
{
    protected Notification()
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
        Metadata.BaseMessageType = BaseMessageType.Notification;
    }
}