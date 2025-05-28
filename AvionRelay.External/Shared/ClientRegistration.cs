namespace AvionRelay.External;

/// <summary>
/// Data for the client we expect to receive from the client itself
/// </summary>
public record ClientRegistration
{
    public required string ClientId { get; init; }
    public required string ClientName { get; init; }
    public required TransportTypes TransportType { get; init; }
    public required Uri HostAddress { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}