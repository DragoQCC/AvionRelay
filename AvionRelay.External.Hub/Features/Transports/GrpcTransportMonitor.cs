using AvionRelay.External.Hub.Components.Connections;

namespace AvionRelay.External.Hub.Features.Transports;

/// <summary>
/// gRPC implementation of ITransportMonitor
/// </summary>
public class GrpcTransportMonitor : ITransportMonitor
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageStatistics _statistics;
    private readonly ILogger<GrpcTransportMonitor> _logger;
    
    public TransportTypes TransportType => TransportTypes.Grpc;
    
    public ClientConnectedEvent ClientConnected { get; set; } = new();
    public ClientDisconnectedEvent ClientDisconnected { get; set; } = new();
    public MessageReceivedEvent MessageReceived { get; set; } = new();
    public MessageSentEvent MessageSent { get; set; } = new();
    
    public GrpcTransportMonitor(ConnectionTracker connectionTracker,MessageStatistics statistics, ILogger<GrpcTransportMonitor> logger)
    {
        _connectionTracker = connectionTracker;
        _statistics = statistics;
        _logger = logger;
    }
    
    public async Task<IEnumerable<ConnectedClient>> GetConnectedClientsAsync()
    {
        var connections = _connectionTracker.GetActiveConnections()
            .Where(c => c.TransportType == TransportType);
            
        return connections.Select(c => new ConnectedClient
        {
            ClientId = c.ConnectionId,
            ClientName = c.ClientName ?? "Anonymous",
            TransportType = TransportType,
            HostAddress = c.HostAddress,
            ConnectedAt = c.ConnectedAt,
            Metadata = c.Metadata
        });
    }
    
    public async Task<bool> DisconnectClientAsync(string clientId)
    {
        // In gRPC, we'd close the streaming connection
        _logger.LogInformation("Disconnecting gRPC client {ClientId}", clientId);
        _connectionTracker.StopTrackingConnection(clientId);
        return true;
    }
    
    public async Task<TransportStatistics> GetStatisticsAsync()
    {
        var snapshot = _statistics.GetSnapshot();
        return new TransportStatistics
        {
            TransportType = TransportType,
            ActiveConnections = _connectionTracker.GetActiveConnections()
                .Count(c => c.TransportType == TransportType),
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
        await ClientConnected.NotifyClientConnected(
            args.ClientId, args.ClientName, args.TransportType, 
            args.HostAddress, args.Metadata);
    }
    
    public async Task RaiseClientDisconnected(ClientDisconnectedEventCall args)
    {
        await ClientDisconnected.NotifyClientDisconnected(args.ClientId, args.Reason);
    }
    
    public async Task RaiseMessageReceived(MessageReceivedEventCall args)
    {
        _statistics.RecordMessageReceived(args.Package.MessageType, args.MessageSize);
        await MessageReceived.NotifyMessageReceived(args.Package, args.FromClientId, args.MessageSize);
    }
    
    public async Task RaiseMessageSent(MessageSentEventCall args)
    {
        await MessageSent.NotifyMessageSent(args.Package, args.ToClientId, args.MessageSize);
    }
}