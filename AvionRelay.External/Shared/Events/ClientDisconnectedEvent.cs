using IntercomEventing.Features.Events;

namespace AvionRelay.External;

public record ClientDisconnectedEvent : GenericEvent<ClientDisconnectedEvent>
{
    private string _clientId;
    private string _reason;
    private DateTime _disconnectedAt;
    
    public async Task NotifyClientDisconnected(string clientId, string reason)
    {
        _clientId = clientId;
        _reason = reason;
        _disconnectedAt = DateTime.UtcNow;
        ClientDisconnectedEventCall call = CreateEventCall();
        await RaiseEvent(call);
    }
    
    /// <inheritdoc />
    override protected ClientDisconnectedEventCall CreateEventCall(params object[]? args) => new()
    {
        ClientId = _clientId,
        Reason = _reason,
        DisconnectedAt = _disconnectedAt
    };
}

public record ClientDisconnectedEventCall : EventCall<ClientDisconnectedEvent>
{
    public required string ClientId { get; init; }
    public required string Reason { get; init; }
    public DateTime DisconnectedAt { get; init; }
}
