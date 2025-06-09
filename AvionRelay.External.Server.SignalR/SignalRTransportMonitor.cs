using AvionRelay.External.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace AvionRelay.External.Server.SignalR;

/// <summary>
/// SignalR implementation of ITransportHub
/// </summary>
public class SignalRTransportMonitor : ITransportMonitor
{
    private readonly IHubContext<AvionRelaySignalRTransport> _hubContext;
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageStatistics _statistics;

    public TransportTypes TransportType => TransportTypes.SignalR;
    
    public ClientConnectedEvent ClientConnected { get; set; } = new();
    public ClientDisconnectedEvent ClientDisconnected { get; set; } = new();
    public MessageReceivedEvent MessageReceived { get; set; } = new();
    public MessageSentEvent MessageSent { get; set; } = new();
    
    public SignalRTransportMonitor(IHubContext<AvionRelaySignalRTransport> hubContext, ConnectionTracker connectionTracker, MessageStatistics statistics)
    {
        _hubContext = hubContext;
        _connectionTracker = connectionTracker;
        _statistics = statistics;
    }
    
    public async Task<IEnumerable<ConnectedClient>> GetConnectedClientsAsync()
    {
        var connections = _connectionTracker.GetActiveConnections();
        return connections.Select(c => new ConnectedClient
        {
            ClientId = c.ClientId,
            ClientName = c.ClientName ?? "Anonymous",
            TransportType = TransportType,
            HostAddress = c.HostAddress,
            ConnectedAt = c.ConnectedAt,
            Metadata = c.Metadata
        });
    }
    
    public async Task<bool> DisconnectClientAsync(string clientId)
    {
        await _hubContext.Clients.Client(clientId).SendAsync("ForceDisconnect");
        return true;
    }
    
    public async Task<TransportStatistics> GetStatisticsAsync()
    {
        var snapshot = _statistics.GetSnapshot();
        return new TransportStatistics
        {
            TransportType = TransportType,
            ActiveConnections = _connectionTracker.GetConnectionCount(),
            TotalMessagesReceived = snapshot.TotalMessagesReceived,
            TotalMessagesSent = snapshot.TotalMessagesSent,
            TotalBytesReceived = snapshot.TotalBytesReceived,
            TotalBytesSent = snapshot.TotalBytesSent,
            MessageTypeCounts = snapshot.MessageTypeStats.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.Count),
            //TODO: Figure out if I need transport and Hub stats and how they differ
            //StartTime = _statistics.StartTime
        };
    }
    

    public async Task RaiseClientConnected(ClientConnectedEventCall args)
    {
        await ClientConnected.NotifyClientConnected(args.ClientId, args.ClientName, args.TransportType,args.HostAddress, args.Metadata);
    }
        
    public async Task RaiseClientDisconnected(ClientDisconnectedEventCall args)
    {
        await ClientDisconnected.NotifyClientDisconnected(args.ClientId, args.Reason);
    }
    
    public async Task RaiseMessageReceived(MessageReceivedEventCall args)
    {
        await MessageReceived.NotifyMessageReceived(args.Package, args.FromClientId, args.MessageSize);
    }
    
    public async Task RaiseMessageSent(MessageSentEventCall args)
    {
        await MessageSent.NotifyMessageSent(args.Package, args.ToClientId, args.MessageSize);
    }
}