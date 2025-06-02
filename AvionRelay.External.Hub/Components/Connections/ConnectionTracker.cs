using System.Collections.Concurrent;

namespace AvionRelay.External.Hub.Components.Connections;

public class ConnectionTracker
{
    public ConcurrentDictionary<string, string> TransportIDLink = new();
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
    private readonly ILogger<ConnectionTracker> _logger;

    public ConnectionTracker(ILogger<ConnectionTracker> logger)
    {
        _logger = logger;
    }

    public void TrackNewConnection(string connectionId, string clientName, TransportTypes transportType, Uri hostAddress, Dictionary<string, object>? metadata = null)
    {
        _connections[connectionId] = new ClientConnection(
            connectionId,
            clientName,
            DateTime.UtcNow,
            transportType,
            ClientConnectionState.Connected,
            hostAddress,
            metadata ?? new Dictionary<string, object>()
        );
        _logger.LogInformation("Client connected: {ConnectionId}", connectionId);
    }

    public void TrackTransportToClientID(string transportId, string clientId)
    {
        TransportIDLink[transportId] = clientId;
    }

    public string GetTransportIDFromClientID(string clientId)
    {
        foreach (var transportIdPair in TransportIDLink)
        {
            if (transportIdPair.Value == clientId)
            {
                return transportIdPair.Key;
            }
        }
        return string.Empty;
    }

    public string GetClientIDFromTransportID(string transportId)
    {
        foreach (KeyValuePair<string, string> transportIdPair in TransportIDLink)
        {
            if (transportIdPair.Key == transportId)
            {
                return transportIdPair.Value;
            }
        }
        return string.Empty;
    }

    public void StopTrackingConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            _logger.LogInformation("Client disconnected: {ConnectionId} after {Duration}",
                                   connectionId,
                                   DateTime.UtcNow - connection.ConnectedAt);
        }
    }

    public void UpdateConnectionTracking(string connectionId, string clientName)
    {
        if (_connections.TryGetValue(connectionId, out var existing))
        {
            _connections[connectionId] = existing with
            {
                ClientName = clientName,
                
            };
        }
    }

    public IEnumerable<ClientConnection> GetActiveConnections() => _connections.Values;
    
    public int GetConnectionCount() => _connections.Count;
    
    public ClientConnection? GetConnection(string connectionId) =>
        _connections.TryGetValue(connectionId, out var conn) ? conn : null;
}