using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using HelpfulTypesAndExtensions;
using Metalama.Framework.Aspects;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using TypedSignalR.Client;

namespace AvionRelay.External.Transports.SignalR;


public class SignalRTransportClient : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly SignalRTransportOptions _options;
    private readonly ILogger<SignalRTransportClient> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IAvionRelaySignalRHubModel? _hubProxy;
    private SignalROnHandler? _onHandler;
    private AvionRelayClient ? _client;

    public MessageResponseReceivedEvent MessageResponseReceivedEvent { get; } = new();
    
    public event Func<Exception?, Task>? Disconnected;
    public event Func<Task>? Connected;
    
    //private readonly ConcurrentDictionary<Guid, TaskCompletionSource<List<ResponsePayload>>> _pendingResponses = new();
    //private readonly ConcurrentDictionary<Guid, ObservableCollection<ResponsePayload>> _pendingResponses = new();
    
    
    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;
    
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
                
            _logger.LogInformation("Connected to SignalR hub at {HubUrl}", _options.HubUrl);
                
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

    public async Task<AvionRelayClient> RegisterClient(ClientRegistrationRequest registrationRequest, CancellationToken cancellationToken = default)
    {
        // Register with the hub
        ClientRegistrationResponse registrationResponse = await _hubProxy.RegisterClient(registrationRequest);
        AvionRelayClient clientInfo = new AvionRelayClient(registrationResponse.ClientId)
        {
            ClientName = registrationRequest.ClientName,
            SupportedMessages = registrationRequest.SupportedMessages,
            Metadata = registrationRequest.Metadata
        };
        _client = clientInfo;
        return clientInfo;
    }
    
    public async Task DisconnectAsync()
    {
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
            await _hubProxy.SendMessage(TransportPackageExtensions.FromPackage(package, _client.ClientID.ToString()));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send package");
            return false;
        }
    }

    /// <summary>
    /// Sends a message to the SignalR hub to transport to clients
    /// </summary>
    /// <param name="package"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    public async Task SendMessageWaitResponse<TResponse>(Package package, List<string> targetHandlers)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot send package - not connected");
            MessagingError error = new MessagingError()
            {
                ErrorMessage = "Failed to send message, this client is not connected to the hub",
                ErrorPriority = MessagePriority.High,
                ErrorTimestamp = DateTime.UtcNow,
                ErrorType = MessageErrorType.Other,
                Source = "Self",
                Suggestion = "Check the hub is running, and that the provided address is correct"
            };
        }
        try
        {
            var transportPackage = TransportPackageExtensions.FromPackage(package, _client.ClientID.ToString());
            if (targetHandlers != null)
            {
                transportPackage.HandlerIdsOrNames.AddRange(targetHandlers);
            }

           await _hubProxy.SendMessageWaitResponse(transportPackage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send package");
            MessagingError error = new MessagingError()
            {
                ErrorMessage = ex.Message,
                ErrorPriority = MessagePriority.High,
                ErrorTimestamp = DateTime.UtcNow,
                ErrorType = MessageErrorType.Other,
                Source = ex.Source ?? "Self"
            };
        }
    }

    public async Task HandleMessageResponse(MessageResponseReceivedEventCall call)
    {
        Guid messageId = call.Responses.First().MessageId;
        List<ResponsePayload> responses = call.Responses;
        _logger.LogInformation("Received {Count} responses for message {MessageId}", responses.Count, messageId);
        if (call.IsFinalResponse)
        {
            _logger.LogInformation("Got all responses for message {MessageId}", messageId);
        }

        await MessageResponseReceivedEvent.RaiseEvent(call);
    }

    public async Task SendMessageResponse(ResponsePayload response)
    {
        await _hubProxy.SendResponse(response);
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