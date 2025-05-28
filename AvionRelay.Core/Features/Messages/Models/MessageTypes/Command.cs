using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages.MessageTypes;

/// <summary>
/// Represents a command, one-to-one, where the receiving side performs an action. <br/>
/// Should be acknowledged and should respond.
/// </summary>
public abstract record Command<TResponse> : AvionRelayMessage, IRespond<TResponse>, ISingleReceiver
{
    protected Command()
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

        Metadata.BaseMessageType = BaseMessageType.Command;
    }
}