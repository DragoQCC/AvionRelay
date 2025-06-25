using System.Collections.Concurrent;
using AvionRelay.Core.Dispatchers;
using AvionRelay.External.Server.Models;
using HelpfulTypesAndExtensions;
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

    private readonly ConcurrentDictionary<string, MessageReceiver> _receivers = new();
    
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
        TrackTransportToClientID(transportId,clientId);
        TrackMessageReceiver(clientId, clientName);
    }

    private void TrackTransportToClientID(string transportId, string clientId)
    {
        _transportIDLink[transportId] = clientId;
    }

    private void TrackMessageReceiver(string clientId, string clientName)
    {
        _receivers.TryAdd(clientId, new MessageReceiver(clientId, clientName));
    }

    public MessageReceiver? GetMessageReceiver(string nameOrId)
    {
        MessageReceiver? receiver = null;
        if (_receivers.TryGetValue(nameOrId, out receiver) is false)
        {
            string? connectionID =  GetClientConnectionIDFromName(nameOrId);
            if (connectionID is not null)
            {
                _receivers.TryGetValue(connectionID, out receiver);
            }
        }
        return receiver;
    }
    
    public ClientConnection? GetConnectionByEitherId(string clientOrTransportId)
    {
        // First try as connection/transport ID
        if (_connections.TryGetValue(clientOrTransportId, out var connection))
        {
            return connection;
        }
    
        // Then try as client ID by looking up transport ID
        var transportId = GetTransportIDFromClientID(clientOrTransportId);
        if (!string.IsNullOrEmpty(transportId) && _connections.TryGetValue(transportId, out connection))
        {
            return connection;
        }
    
        // Finally try reverse - maybe it was a transport ID and we need the client ID
        var clientId = GetClientIDFromTransportID(clientOrTransportId);
        if (!string.IsNullOrEmpty(clientId))
        {
            // Try one more time with the client ID
            foreach (var conn in _connections.Values)
            {
                if (conn.TransportId == clientId)
                {
                    return conn;
                }
            }
        }
        return null;
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
        _logger.LogWarning("Could not find transport id for client with id {ClientId}",clientId);
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

    public ClientConnection? GetConnection(string clientId) => GetConnectionByEitherId(clientId);

    public ClientConnection? GetClientConnectionFromName(string clientName)
    {
        return GetActiveConnections().FirstOrDefault(x => x.ClientName.EqualsCaseInsensitive(clientName));
    }
    
    public string? GetClientConnectionIDFromName(string clientName)
    {
        return GetActiveConnections().FirstOrDefault(x => x.ClientName.EqualsCaseInsensitive(clientName))?.ClientId;
    }

    public string? GetClientNameFromConnectionID(string connectionId)
    {
        return GetActiveConnections().FirstOrDefault(x => x.ClientId.Equals(connectionId))?.ClientName;
    }

    public ClientConnection? GetClientConnectionFromNameOrId(string nameOrId)
    {
        ClientConnection? clientConnection = GetConnectionByEitherId(nameOrId);
        if (clientConnection is null)
        {
            clientConnection = GetClientConnectionFromName(nameOrId);
        }
        return clientConnection;
    }

    public ClientConnection? FilterConnectionsForTargetClient(List<ClientConnection> connections, string nameOrId)
    {
        ClientConnection? clientConnection = null;
        clientConnection = connections.FirstOrDefault(x => x.ClientId.Equals(nameOrId));
        if (clientConnection is null)
        {
            clientConnection = GetClientConnectionFromName(nameOrId);
        }
        return clientConnection;
    }
}