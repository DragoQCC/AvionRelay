using System.Collections.Concurrent;
using System.Text.Json;
using AvionRelay.Core;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Transports.SignalR;

//TODO: Implement the transport client calls
public class AvionRelaySignalRMessageBus : AvionRelayMessageBus
{
    private readonly ILogger<AvionRelaySignalRMessageBus> _logger;
    private readonly SignalRTransportOptions _transportOptions;
    private readonly SignalRTransportClient _client;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Package>> _pendingResponses = new();
    
    public AvionRelaySignalRMessageBus(ILogger<AvionRelaySignalRMessageBus> logger,SignalRTransportOptions transportOptions, SignalRTransportClient client)
    {
        _logger = logger;
        _transportOptions = transportOptions;
        _client = client;
        _client.Connected += OnConnected;
        _client.Disconnected += OnDisconnected;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(cancellationToken);
    }
    
    public async Task StopAsync()
    {
        await _client.DisconnectAsync();
    }

    public override async Task RegisterMessenger(List<string>? supportedMessageNames = null)
    {
        await _client.RegisterClient(supportedMessageNames);
    }
    
    //TODO: Update with the right Response wrapper (i.e. IMessageResponse)
    public override async Task<MessageResponse<TResponse>> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken? cancellationToken = null,TimeSpan? timeout = null)
    {
        var package = Package.Create(command);
        var messageId = package.WrapperID;
        
        var tcs = new TaskCompletionSource<Package>();
        _pendingResponses[messageId] = tcs;
        
        try
        {
            _logger.LogInformation("Sending message {MessageId}", messageId);
            _logger.LogInformation("Client info: ID:{ConnectionID} State:{State}",_client.ConnectionId, _client.State);
            return (await _client.SendMessageWaitResponse<TResponse>(package)).First();
        }
        finally
        {
            _pendingResponses.TryRemove(messageId, out _);
        }
    }

    /// <inheritdoc />
    public override async Task<List<MessageResponse<TResponse>>> RequestInspection<TInspection, TResponse>(TInspection inspection,CancellationToken? cancellationToken = null,TimeSpan? timeout = null)
    {
        Package package = Package.Create(inspection);
        await _client.SendPackageAsync(package, cancellationToken ?? CancellationToken.None);
        //TODO: Same as command i will need to implement the logic for awaiting the responses from the hub
        return [ ];
    }

    public override async Task PublishNotification<TNotification>(TNotification notification, CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
        var package = Package.Create(notification);
        await _client.SendPackageAsync(package, cancellationToken ?? CancellationToken.None);
    }
    
    public override async Task SendAlert<TAlert>(TAlert alert,CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
        var package = Package.Create(alert);
        await _client.SendPackageAsync(package, cancellationToken ?? CancellationToken.None);
    }

    /// <inheritdoc />
    public override async Task RespondToMessage<T, TResponse>(Guid messageId, TResponse response, MessageReceiver responder)
    {
        _logger.LogInformation("Sending response for message {messageID}", messageId);
        JsonResponse responseWrapper = new()
        {
            MessageId = messageId,
            ResponseJson = JsonSerializer.Serialize(response),
            Acknowledger = responder
        };
        await _client.SendMessageResponse(responseWrapper);
    }

    /// <inheritdoc />
    public override async Task AcknowledgeMessage<T>(Guid messageId, MessageReceiver acknowledger)
    {
    }

    /// <inheritdoc />
    public override async Task<Package?> ReadNextOutboundMessage(CancellationToken cancellationToken = default) => null;

    private async Task HandleReceivedPackage(Package package)
    {
        _logger.LogDebug("Handling received package: {MessageId} of type {MessageType}", package.WrapperID, package.MessageType);
            
        // Check if this is a response to a pending command
        if (_pendingResponses.TryRemove(package.WrapperID, out var tcs))
        {
            tcs.SetResult(package);
            return;
        }
        
        // Otherwise, process through the messaging manager
        await MessageHandlerRegister.ProcessPackage(package);
    }
    
    private Task OnConnected()
    {
        _logger.LogInformation("SignalR transport connected");
        return Task.CompletedTask;
    }
    
    private Task OnDisconnected(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR transport disconnected");
        
        // Cancel all pending responses
        foreach (var pending in _pendingResponses.Values)
        {
            pending.TrySetCanceled();
        }
        _pendingResponses.Clear();
        
        return Task.CompletedTask;
    }
}
