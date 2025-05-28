using AvionRelay.Core.Messages;
using HelpfulTypesAndExtensions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
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
    
    public event Func<Package, Task>? PackageReceived;
    public event Func<Exception?, Task>? Disconnected;
    public event Func<Task>? Connected;
    
    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;
    public string ConnectionId => _hubConnection?.ConnectionId ?? string.Empty;
    
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
            
            // Register with the hub
            await _hubProxy.RegisterClient(new ClientRegistration
            {
                ClientId = _options.ClientId,
                ClientName = _options.ClientName,
                TransportType = TransportTypes.SignalR,
                HostAddress = Helper.GetPreferredIPAddress()
            });
            
           
                
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
            await _hubConnection.InvokeAsync("SendMessage", 
                new RoutedMessage
                {
                    Package = package,
                    SenderId = _options.ClientId,
                    MessageId = Guid.NewGuid()
                }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send package");
            return false;
        }
    }
    
    private HubConnection BuildHubConnection()
    {
        var builder = new HubConnectionBuilder().WithUrl(
                _options.HubUrl, options =>
                {
                    //options.CloseTimeout = TimeSpan.FromSeconds(30);
                }
            )
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

        _hubConnection.On<Package>("ReceivePackage", async package =>
        {
            _logger.LogDebug("Received package of type {MessageType}", package.MessageType);
            if (PackageReceived != null)
            {
                await PackageReceived.Invoke(package);
            }
        });
        
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