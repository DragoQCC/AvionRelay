using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages.MessageTypes;


/// <summary>
/// Represents a request from one sender to multiple receivers. (e.g., a status check or data request) <br/>
/// One-to-many, should be acknowledged, should respond.
/// </summary>
public abstract record Inspection<TResponse> : AvionRelayMessage, IRespond<TResponse>, IMultiReceiver
{
    protected Inspection()
    {
        AllowedStates = new List<MessageState>
        {
            new MessageState.Created(),
            new MessageState.Sent(),
            new MessageState.Received(),
            new MessageState.Processing(),
            new MessageState.Responded(),
            new FinalizedMessageState.ResponseReceived(),
            new FinalizedMessageState.Failed(),
        };

        Metadata.BaseMessageType = BaseMessageType.Inspection;
    }
}