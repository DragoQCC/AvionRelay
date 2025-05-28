namespace AvionRelay.External.Hub.Components.Connections;


public record ClientConnection(
    string ConnectionId,
    string? ClientName,
    DateTime ConnectedAt,
    TransportTypes TransportType,
    ClientConnectionState ConnectionState,
    Uri HostAddress,
    Dictionary<string, object> Metadata
);

public enum ClientConnectionState
{
    Connecting,
    Connected,
    Disconnected,
    Reconnecting,
}
