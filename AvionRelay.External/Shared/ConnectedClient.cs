namespace AvionRelay.External;

public class ConnectedClient
{
    public required string ClientId { get; init; }
    public required string ClientName { get; init; }
    public required TransportTypes TransportType { get; init; }
    public required Uri HostAddress { get; init; }
    public DateTime ConnectedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}