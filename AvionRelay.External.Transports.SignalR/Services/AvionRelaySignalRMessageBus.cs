using System.Collections.Concurrent;
using System.Text.Json;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using HelpfulTypesAndExtensions;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Transports.SignalR;

//TODO: Implement the transport client calls
public class AvionRelaySignalRMessageBus : AvionRelayExternalBus
{
    private readonly ILogger<AvionRelaySignalRMessageBus> _logger;
    private readonly SignalRTransportOptions _transportOptions;
    private readonly SignalRTransportClient _transportClient;
    private readonly ConcurrentDictionary<Guid, List<TaskCompletionSource<ResponsePayload>>> _pendingResponses = new();
    
    public AvionRelaySignalRMessageBus(ILogger<AvionRelaySignalRMessageBus> logger,SignalRTransportOptions transportOptions, SignalRTransportClient transportClient)
    {
        _logger = logger;
        _transportOptions = transportOptions;
        _transportClient = transportClient;
        _transportClient.Connected += OnConnected;
        _transportClient.Disconnected += OnDisconnected;
        _transportClient.MessageResponseReceivedEvent.Subscribe<MessageResponseReceivedEventCall>(HandleMessageResponse);
    }
    
    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _transportClient.ConnectAsync(cancellationToken);
    }
    
    public override async Task StopAsync()
    {
        await _transportClient.DisconnectAsync();
    }

    /// <inheritdoc />
    public override async Task<AvionRelayClient> RegisterClient(string clientName, string clientVersion, List<string> supportedMessageNames, Dictionary<string, object>? metadata = null)
    {
        ClientRegistrationRequest registrationRequest = new()
        {
            ClientName = clientName,
            ClientVersion = clientVersion,
            TransportType = TransportTypes.SignalR,
            HostAddress = Helper.GetPreferredIPAddress(),
            Metadata = metadata ?? new Dictionary<string, object>(),
            SupportedMessages = supportedMessageNames
        };
        return await _transportClient.RegisterClient(registrationRequest);
    }
    

    public override async Task<ResponsePayload<TResponse>> ExecuteCommand<TCommand, TResponse>(TCommand command, string targetHandler, CancellationToken? cancellationToken = null,TimeSpan? timeout = null)
    {
        var package = Package.Create(command);
        var messageId = package.WrapperID;
        
        var tcs = new TaskCompletionSource<ResponsePayload>();
        _pendingResponses.TryAdd(messageId,[tcs]);
        
        try
        {
            _logger.LogInformation("Sending message {MessageId}", messageId);
            await _transportClient.SendMessageWaitResponse<TResponse>(package, [ targetHandler ]);
            var untypedResponse = await _pendingResponses[messageId].First().Task;
            return ResponsePayload<TResponse>.FromResponsePayload(untypedResponse);
        }
        finally
        {
            _pendingResponses.TryRemove(messageId, out _);
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ResponsePayload<TResponse>> RequestInspection<TInspection, TResponse>(TInspection inspection, List<string> targetHandlers, CancellationToken? cancellationToken = null,TimeSpan? timeout = null)
    {
        Package package = Package.Create(inspection);
        var messageId = package.WrapperID;
        
        //for each handler, add a task completion source to keep track of 
        var tcsList = new List<TaskCompletionSource<ResponsePayload>>(targetHandlers.Count);
        targetHandlers.ForEach(x => tcsList.Add(new TaskCompletionSource<ResponsePayload>()));
        _pendingResponses.TryAdd(messageId, tcsList);
        _logger.LogInformation("Sending message {MessageId}", messageId);
            
        _transportClient.SendMessageWaitResponse<TResponse>(package, targetHandlers).FireAndForget();
        _logger.LogDebug("Waiting on {PendingTasks} responses to come back", _pendingResponses[messageId].Count);
        await foreach (var pendingResponseTask in Task.WhenEach(_pendingResponses[messageId].Select(x => x.Task)))
        {
            ResponsePayload resp = await pendingResponseTask;
            var typedResponse = ResponsePayload<TResponse>.FromResponsePayload(resp);
            _logger.LogDebug("Yielding response back to inspection requester");
            yield return typedResponse;
        }
    }

    /// <inheritdoc />
    public override async Task PublishNotification<TNotification>(TNotification notification, CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
        var package = Package.Create(notification);
        await _transportClient.SendPackageAsync(package, cancellationToken ?? CancellationToken.None);
    }
    
    /// <inheritdoc />
    public override async Task SendAlert<TAlert>(TAlert alert,CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
        var package = Package.Create(alert);
        await _transportClient.SendPackageAsync(package, cancellationToken ?? CancellationToken.None);
    }

    /// <inheritdoc />
    public override async Task RespondToMessage<T, TResponse>(Guid messageID,TResponse response, MessageReceiver responder)
    {
        _logger.LogInformation("Sending response for message {messageID}", messageID.ToString());
        string jsonResponse = response.ToJsonIgnoreCase();
        ResponsePayload responseWrapper = new(messageID, responder, DateTime.UtcNow, jsonResponse);
        await _transportClient.SendMessageResponse(responseWrapper);
    }

    /// <inheritdoc />
    public override async Task AcknowledgeMessage<T>(Guid messageId, MessageReceiver acknowledger)
    {
        //await _transportClient.
    }
    
    public async Task HandleMessageResponse(MessageResponseReceivedEventCall call)
    {
        try
        {
            Guid messageId = call.Responses.First().MessageId;
            List<ResponsePayload> responses = call.Responses;
            _logger.LogInformation("Received {Count} responses for message {MessageId}", responses.Count, messageId);
            if (call.IsFinalResponse)
            {
                _logger.LogInformation("Got all responses for message {MessageId}", messageId);
            }
                
            if (_pendingResponses.TryGetValue(messageId, out var taskCompletionSources))
            {
                //TODO: Since this is called multiple times its setting a result on the first item more then once
                foreach (var response in responses)
                {
                    var firstNonCompleteTcs = taskCompletionSources.First(x => x.Task.IsCompleted is false);
                    firstNonCompleteTcs.TrySetResult(response);
                }
                /*await Task.Delay(500);
                taskCompletionSources.RemoveRange(0,responses.Count);*/
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    

    /*private async Task HandleReceivedPackage(Package package)
    {
        _logger.LogDebug("Handling received package: {MessageId} of type {MessageType}", package.WrapperID, package.Message.Metadata.MessageTypeName);
            
        // Check if this is a response to a pending command
        if (_pendingResponses.TryRemove(package.WrapperID, out var tcs))
        {
            tcs.SetResult(package);
            return;
        }
        
        // Otherwise, process through the messaging manager
        await MessageHandlerRegister.ProcessPackage(package);
    }*/
    
    private Task OnConnected()
    {
        _logger.LogInformation("SignalR transport connected");
        return Task.CompletedTask;
    }
    
    private Task OnDisconnected(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR transport disconnected");
        
        // Cancel all pending responses
        foreach (var pendingList in _pendingResponses.Values)
        {
            foreach (TaskCompletionSource<ResponsePayload> taskCompletionSource in pendingList)
            {
                taskCompletionSource.TrySetCanceled();
            }
        }
        _pendingResponses.Clear();
        return Task.CompletedTask;
    }


}
