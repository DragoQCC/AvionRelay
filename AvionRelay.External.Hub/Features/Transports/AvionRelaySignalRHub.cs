using System.Text;
using AvionRelay.Core;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using AvionRelay.External.Hub.Components.Connections;
using AvionRelay.External.Hub.Services;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace AvionRelay.External.Hub.Features.Transports;

/// <summary>
/// The actual SignalR Hub that clients connect to
/// </summary>
public partial class AvionRelaySignalRHub : Hub<IAvionRelaySignalRClientModel>, IAvionRelaySignalRHubModel
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly SignalRMessageStatistics _statistics;
    private readonly SignalRTransportMonitor _monitor;
    
    public AvionRelaySignalRHub(ConnectionTracker connectionTracker, SignalRMessageStatistics statistics, SignalRTransportMonitor monitor)
    {
        _connectionTracker = connectionTracker;
        _statistics = statistics;
        _monitor = monitor;
    }
    
    public override async Task OnConnectedAsync()
    {
        //todo: ILogger cannot be created or injected into the hub so a different work around will be needed :/
        Console.WriteLine("Client connected: " + Context.ConnectionId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.StopTrackingConnection(Context.ConnectionId);
        
        await _monitor.RaiseClientDisconnected(new ClientDisconnectedEventCall()
        {
            ClientId = Context.ConnectionId,
            DisconnectedAt = DateTime.UtcNow,
            Reason = exception?.Message
        });
        
        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task SendMessage(RoutedMessage message)
    {
        //get the size of the message.Package.Message in bytes
        int messageSize = Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(message.Package.Message));
        
        _statistics.RecordMessageReceived(message.Package.MessageType.ToString(),messageSize);
        
        await _monitor.RaiseMessageReceived(new MessageReceivedEventCall()
        {
            Package = message.Package,
            FromClientId = Context.ConnectionId,
            MessageSize = messageSize
        });
        
        // Route message logic...
    }

    /// <inheritdoc />
    public async Task SendPackage(Package package)
    {
    }

    /// <inheritdoc />
    public async Task<bool> RegisterReceiver(ExternalMessageReceiver receiver) => false;

    /// <inheritdoc />
    public async Task RegisterClient(ClientRegistration clientRegistration)
    {
        _connectionTracker.TrackNewConnection(clientRegistration.ClientId, clientRegistration.ClientName, clientRegistration.TransportType, clientRegistration.HostAddress, clientRegistration.Metadata);
        
        await _monitor.RaiseClientConnected(new ClientConnectedEventCall()
        {
            ClientId = Context.ConnectionId,
            ClientName = clientRegistration.ClientName,
            TransportType = clientRegistration.TransportType,
            HostAddress = clientRegistration.HostAddress,
            Metadata = clientRegistration.Metadata
        });
    }
}

//This portion of the partial class will hold all the methods that the hub can invoke on clients
public partial class AvionRelaySignalRHub
{
    
}