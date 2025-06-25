using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;

namespace AvionRelay.External.Transports.Grpc;

public class AvionRelayGrpcMessageBus : AvionRelayMessageBus
{
    /// <inheritdoc />
    public override async Task RegisterMessenger(List<string> supportedMessageNames)
    {
    }

    /// <inheritdoc />
    public override async Task<MessageResponse<TResponse>> ExecuteCommand<TCommand, TResponse>(TCommand command, string? targetHandler = null, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) => null;

    /// <inheritdoc />
    public override async Task<List<MessageResponse<TResponse>>> RequestInspection<TInspection, TResponse>(TInspection inspection, List<string>? targetHandlers = null, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) => null;

    /// <inheritdoc />
    public override async Task PublishNotification<TNotification>(TNotification notification, CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
    }

    /// <inheritdoc />
    public override async Task SendAlert<TAlert>(TAlert alert, CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
    }

    /// <inheritdoc />
    public override async Task RespondToMessage<T, TResponse>(Guid messageId, TResponse response, MessageReceiver responder)
    {
    }

    /// <inheritdoc />
    public override async Task AcknowledgeMessage<T>(Guid messageId, MessageReceiver acknowledger)
    {
    }

    /// <inheritdoc />
    public override async Task<Package?> ReadNextOutboundMessage(CancellationToken cancellationToken = default) => null;
}