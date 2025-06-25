using IntercomEventing.Features.Events;

namespace AvionRelay.External;

public record ClientConnectedEvent : GenericEvent<ClientConnectedEvent>
{
    private string _clientId;
    private string _clientName;
    private TransportTypes _transportType;
    private Uri _hostAddress;
    private DateTime _connectedAt;
    private Dictionary<string, object> _metadata;
    
    /// <summary>
    /// Creates the client connected event
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientName"></param>
    /// <param name="groupId"></param>
    /// <param name="metadata"></param>
    public async Task NotifyClientConnected(string clientId, string clientName, TransportTypes transportType, Uri hostAddress, Dictionary<string, object> metadata = null!)
    {
        _clientId = clientId;
        _clientName = clientName;
        _transportType = transportType;
        _hostAddress = hostAddress;
        _connectedAt = DateTime.UtcNow;
        _metadata = metadata;
        ClientConnectedEventCall call = CreateEventCall();
        await RaiseEvent(call);
    }

    /// <inheritdoc />
    override protected ClientConnectedEventCall CreateEventCall() => new()
    {
        ClientId = _clientId,
        ClientName = _clientName,
        TransportType = _transportType,
        HostAddress = _hostAddress,
        ConnectedAt = _connectedAt,
        Metadata = _metadata
    };
}

public record ClientConnectedEventCall : EventCall<ClientConnectedEvent>
{
    public required string ClientId { get; init; }
    public required string ClientName { get; init; }
    public required TransportTypes TransportType { get; init; }
    public required Uri HostAddress { get; init; }
    public DateTime ConnectedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}