using AvionRelay.Core.Dispatchers;
using Newtonsoft.Json;

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
            MessageState.Created,
            MessageState.Sent,
            MessageState.Received,
            MessageState.Processing,
            MessageState.Responded,
            MessageState.ResponseReceived,
            MessageState.Failed,
        };

        Metadata.BaseMessageType = BaseMessageType.Command;
    }
}