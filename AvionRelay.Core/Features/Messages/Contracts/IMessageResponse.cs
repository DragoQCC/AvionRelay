using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages;

public interface IMessageResponse
{
    /// <summary>
    /// The ID of the message that is being responded to
    /// </summary>
    public Guid MessageId { get; }
    
    /// <summary>
    /// The acknowledger or responder to the message
    /// </summary>
    public MessageReceiver Acknowledger { get; }
}

public class MessageAcknowledgement : IMessageResponse
{
    public Guid MessageId { get; internal set; }

    /// <inheritdoc />
    public MessageReceiver Acknowledger { get; internal set; }
}

public class MessageResponse<TResponse> : IMessageResponse
{
    public Guid MessageId { get; internal set; }

    /// <inheritdoc />
    public MessageReceiver Acknowledger { get; internal set; }
    
    public TResponse Response { get; set; }
}