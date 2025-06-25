namespace AvionRelay.External;

public interface ITransportMonitor
{
    TransportTypes TransportType { get; }
    ClientConnectedEvent ClientConnected { get; protected set; }
    ClientDisconnectedEvent ClientDisconnected { get; protected set; }
    MessageReceivedEvent MessageReceived { get; protected set; }
    MessageSentEvent MessageSent { get; protected set; }
    
    Task<IEnumerable<ConnectedClient>> GetConnectedClientsAsync();
    Task<bool> DisconnectClientAsync(string clientId);
    Task<TransportStatistics> GetStatisticsAsync();

    public Task RaiseClientConnected(ClientConnectedEventCall args);

    public Task RaiseClientDisconnected(ClientDisconnectedEventCall args);

    public Task RaiseMessageReceived(MessageReceivedEventCall args);

    public Task RaiseMessageSent(MessageSentEventCall args);
}

