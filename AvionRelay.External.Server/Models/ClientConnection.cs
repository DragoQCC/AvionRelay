namespace AvionRelay.External.Server.Models;


public record ClientConnection(
    string ClientId,
    string TransportId,
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
