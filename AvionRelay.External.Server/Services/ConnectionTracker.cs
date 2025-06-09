using System.Collections.Concurrent;
using AvionRelay.External.Server.Models;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.Services;

public class ConnectionTracker
{
    /// <summary>
    /// Key: The transport-specific ID, Value: the client ID 
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _transportIDLink = new();
    
    /// <summary>
    /// Key: Client ID Value: The client connection info
    /// </summary>
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();
    
    private readonly ILogger<ConnectionTracker> _logger;

    public ConnectionTracker(ILogger<ConnectionTracker> logger)
    {
        _logger = logger;
    }

    public void TrackNewConnection(string clientId,string transportId, string clientName, TransportTypes transportType, Uri hostAddress, Dictionary<string, object>? metadata = null)
    {
        _connections[clientId] = new ClientConnection(
            clientId,
            transportId,
            clientName,
            DateTime.UtcNow,
            transportType,
            ClientConnectionState.Connected,
            hostAddress,
            metadata ?? new Dictionary<string, object>()
        );
        _logger.LogInformation("Client connected: {ConnectionId}", clientId);
    }

    public void TrackTransportToClientID(string transportId, string clientId)
    {
        _transportIDLink[transportId] = clientId;
    }

    public string GetTransportIDFromClientID(string clientId)
    {
        foreach (var transportIdPair in _transportIDLink)
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
        foreach (KeyValuePair<string, string> transportIdPair in _transportIDLink)
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

    public void UpdateConnectionTracking(string clientID, string clientName)
    {
        if (_connections.TryGetValue(clientID, out var existing))
        {
            _connections[clientID] = existing with
            {
                ClientName = clientName,
                
            };
        }
    }

    public IEnumerable<ClientConnection> GetActiveConnections() => _connections.Values;
    
    public int GetConnectionCount() => _connections.Count;
    
    public ClientConnection? GetConnection(string clientId) =>
        _connections.TryGetValue(clientId, out var conn) ? conn : null;
}