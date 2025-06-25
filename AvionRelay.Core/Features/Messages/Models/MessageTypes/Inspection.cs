namespace AvionRelay.Core.Messages.MessageTypes;


/// <summary>
/// Represents a request from one sender to multiple receivers. (e.g., a status check or data request) <br/>
/// One-to-many, should be acknowledged, should respond.
/// </summary>
public abstract class Inspection<TResponse> : AvionRelayMessage, IRespond<TResponse>, IMultiReceiver
{
    protected Inspection()
    {
        AllowedStates = new List<MessageState>
        {
            MessageState.Created,
            MessageState.Sent,
            MessageState.Received,
            MessageState.Processing,
            MessageState.Responded,
            MessageState.ResponseReceived,
            MessageState.Failed,
        };

        Metadata.BaseMessageType = BaseMessageType.Inspection;
    }
}