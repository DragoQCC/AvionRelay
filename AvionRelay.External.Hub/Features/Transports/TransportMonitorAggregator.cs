using AvionRelay.Core.Services;
using AvionRelay.External.Hub.Components.Statistics;

namespace AvionRelay.External.Hub.Features.Transports;


/// <summary>
/// Aggregates data from multiple transport hubs
/// </summary>
public class TransportMonitorAggregator
{
    private readonly IEnumerable<ITransportMonitor> _transportHubs;
    private readonly IMessageStorage _messageStorage;
    private readonly ILogger<TransportMonitorAggregator> _logger;
    
    //These act as a way to merge the events from multiple hubs into a single event, so for example, the UI just needs to subscribe to this 1 event
    public ClientConnectedEvent ClientConnected = new();
    public ClientDisconnectedEvent ClientDisconnected = new();
    public MessageReceivedEvent MessageReceived = new();
    public MessageSentEvent MessageSent = new();
    
    public TransportMonitorAggregator(IEnumerable<ITransportMonitor> transportHubs, IMessageStorage messageStorage, ILogger<TransportMonitorAggregator> logger)
    {
        _transportHubs = transportHubs;
        _messageStorage = messageStorage;
        _logger = logger;
        
        // Subscribe to all transport events
        foreach (var hub in _transportHubs)
        {
            hub.ClientConnected.Subscribe<ClientConnectedEventCall>(OnClientConnected);
            hub.ClientDisconnected.Subscribe<ClientDisconnectedEventCall>(OnClientDisconnected);
            hub.MessageReceived.Subscribe<MessageReceivedEventCall>(OnMessageReceived);
            hub.MessageSent.Subscribe<MessageSentEventCall>(OnMessageSent);
        }
    }
    
   
    
    public async Task<IEnumerable<ConnectedClient>> GetAllConnectedClientsAsync()
    {
        var allClients = new List<ConnectedClient>();
        
        foreach (var hub in _transportHubs)
        {
            try
            {
                var clients = await hub.GetConnectedClientsAsync();
                allClients.AddRange(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get clients from {TransportType}", hub.TransportType);
            }
        }
        
        return allClients;
    }
    
    public async Task<AggregatedStatistics> GetAggregatedStatisticsAsync()
    {
        var stats = new AggregatedStatistics();
        
        foreach (var hub in _transportHubs)
        {
            try
            {
                var transportStats = await hub.GetStatisticsAsync();
                stats.TransportStatistics[hub.TransportType] = transportStats;
                
                // Aggregate totals
                stats.TotalActiveConnections += transportStats.ActiveConnections;
                stats.TotalMessagesReceived += transportStats.TotalMessagesReceived;
                stats.TotalMessagesSent += transportStats.TotalMessagesSent;
                stats.TotalBytesReceived += transportStats.TotalBytesReceived;
                stats.TotalBytesSent += transportStats.TotalBytesSent;
                
                // Aggregate message types
                foreach (var (messageType, count) in transportStats.MessageTypeCounts)
                {
                    stats.MessageTypeCountsTotal.TryGetValue(messageType, out var currentCount);
                    stats.MessageTypeCountsTotal[messageType] = currentCount + count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get statistics from {TransportType}", hub.TransportType);
            }
        }
        
        return stats;
    }
    
    public async Task<IEnumerable<StoredMessage>> GetRecentMessagesAsync(int count = 100)
    {
        //TODO: Get recent messages from storage, currently the storage holds packages not Stored Messages so might need to change that
        return await Task.FromResult(Enumerable.Empty<StoredMessage>());
    }
    
    private async Task OnClientConnected(ClientConnectedEventCall e)
    {
        await ClientConnected.NotifyClientConnected(e.ClientId, e.ClientName, e.TransportType, e.HostAddress, e.Metadata);
    }
    
    private async Task OnClientDisconnected(ClientDisconnectedEventCall e)
    {
        await ClientDisconnected.NotifyClientDisconnected(e.ClientId, e.Reason);
    }

    private async Task OnMessageReceived(MessageReceivedEventCall e)
    {
        //TODO: Store message
        
        
        await MessageReceived.NotifyMessageReceived(e.Package, e.FromClientId, e.MessageSize);
    }
    
    private async Task OnMessageSent(MessageSentEventCall e)
    {
        await MessageSent.NotifyMessageSent(e.Package, e.ToClientId, e.MessageSize);
    }
}