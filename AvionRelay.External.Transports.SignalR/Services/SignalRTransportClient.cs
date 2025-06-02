using System.Collections.Concurrent;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;
using AvionRelay.Core.Services;
using HelpfulTypesAndExtensions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TypedSignalR.Client;

namespace AvionRelay.External.Transports.SignalR;

//TODO: Implement strongly typed Hub and Client to avoid method calls via Strings
public class SignalRTransportClient : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly SignalRTransportOptions _options;
    private readonly ILogger<SignalRTransportClient> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private CancellationTokenSource? _reconnectCts;
    private IAvionRelaySignalRHubModel? _hubProxy;
    private SignalROnHandler? _onHandler;
    
    public event Func<Exception?, Task>? Disconnected;
    public event Func<Task>? Connected;
    
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<List<MessageResponse<object>>>> _pendingResponses = new();
    
    
    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;
    public string ConnectionId => _options.ClientId ?? string.Empty;
    
    public SignalRTransportClient(SignalRTransportOptions options, ILogger<SignalRTransportClient> logger)
    {
        _options = options;
        _logger = logger;
        ConnectAsync().FireAndForget();
    }
    
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                return true;
            }

            _hubConnection = BuildHubConnection();
            SetupEventHandlers();
            
            await _hubConnection.StartAsync(cancellationToken);
            _hubProxy = _hubConnection.CreateHubProxy<IAvionRelaySignalRHubModel>(cancellationToken);
            _onHandler = new SignalROnHandler();
            _hubConnection.Register<IAvionRelaySignalRClientModel>(_onHandler);
            await _onHandler.MessageResponseReceivedEvent.Subscribe<MessageResponseReceivedEventCall>(HandleMessageResponse);
                
            _logger.LogInformation("Connected to SignalR hub at {HubUrl} as {ClientName}", _options.HubUrl, _options.ClientName);
                
            if (Connected != null)
            {
                await Connected.Invoke();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub");
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task RegisterClient(List<string>? supportedMessageNames = null, CancellationToken cancellationToken = default)
    {
        // Register with the hub
        await _hubProxy.RegisterClient(new ClientRegistration
        {
            ClientId = _options.ClientId,
            ClientName = _options.ClientName,
            TransportType = TransportTypes.SignalR,
            HostAddress = Helper.GetPreferredIPAddress(),
            SupportedMessages = supportedMessageNames ?? []
        });
    }
    
    public async Task DisconnectAsync()
    {
        _reconnectCts?.Cancel();
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
    
    public async Task<bool> SendPackageAsync(Package package, CancellationToken cancellationToken = default)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot send package - not connected");
            return false;
        }
        try
        {
            /*await _hubProxy?.SendMessage(new RoutedMessage
                {
                    Package = package,
                    SenderId = _options.ClientId,
                    MessageName = nameof(package.MessageType)
                });*/
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send package");
            return false;
        }
    }

    public async Task<List<MessageResponse<TResponse>>> SendMessageWaitResponse<TResponse>(Package package)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot send package - not connected");
            return [ ];
        }

        try
        {
            var tcs = new TaskCompletionSource<List<MessageResponse<object>>>();
            _pendingResponses[package.WrapperID] = tcs;

            await _hubProxy.SendMessageWaitResponse(TransportPackage.FromPackage(package,ConnectionId));

            //wait for the result to be set 
            var untypedResponses = await tcs.Task;
            List<MessageResponse<TResponse>> responses = untypedResponses.Cast<MessageResponse<TResponse>>().ToList();
            return responses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send package");
            return [ ];
        }
        finally
        {
            _pendingResponses.TryRemove(package.WrapperID, out _);
        }
    }

    public async Task HandleMessageResponse(MessageResponseReceivedEventCall call)
    {
        Guid messageId = call.messageId;
        var responses = call.responses;
        _logger.LogInformation("Received {Count} responses for message {MessageId}", responses.Count, messageId);
                
        if (_pendingResponses.TryRemove(messageId, out var tcs))
        {
            tcs.TrySetResult(responses);
        }
    }

    public async Task SendMessageResponse(Guid messageID, object response)
    {
        await _hubProxy.SendResponse(messageID, response);
    }
    
    private HubConnection BuildHubConnection()
    {
        var builder = new HubConnectionBuilder()
            .WithUrl(_options.HubUrl)
            .WithAutomaticReconnect(new CustomRetryPolicy(_options.Reconnection))
            .WithStatefulReconnect()
            .WithServerTimeout(TimeSpan.FromSeconds(120));
        
            
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug));
        }

        /*builder.AddNewtonsoftJsonProtocol(opt =>
        {
            opt.PayloadSerializerSettings.TypeNameHandling = TypeNameHandling.All;
        });*/
        
        return builder.Build();
    }
    
    private void SetupEventHandlers()
    {
        if (_hubConnection == null)
        {
            return;
        }
        _hubConnection.Closed += OnConnectionClosed;
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;
    }
    
    private async Task OnConnectionClosed(Exception? exception)
    {
        _logger.LogWarning(exception, "Connection closed");
        if (Disconnected != null)
        {
            await Disconnected.Invoke(exception);
        }
    }
    
    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogInformation("Attempting to reconnect...");
        return Task.CompletedTask;
    }
    
    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("Reconnected with connection ID: {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock.Dispose();
    }
    
    private class CustomRetryPolicy : IRetryPolicy
    {
        private readonly SignalRTransportOptions.ReconnectionPolicy _policy;
        private int _retryCount = 0;
        
        public CustomRetryPolicy(SignalRTransportOptions.ReconnectionPolicy policy)
        {
            _policy = policy;
        }
        
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            if (_retryCount >= _policy.MaxAttempts)
            {
                return null;
            }

            _retryCount++;
            var delay = TimeSpan.FromMilliseconds(
                Math.Min(
                    _policy.InitialDelay.TotalMilliseconds * Math.Pow(2, _retryCount - 1),
                    _policy.MaxDelay.TotalMilliseconds
                )
            );
            
            return delay;
        }
    }
}