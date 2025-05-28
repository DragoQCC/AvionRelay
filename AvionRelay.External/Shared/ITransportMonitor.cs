using AvionRelay.Core.Messages;
using HelpfulTypesAndExtensions;
using IntercomEventing.Features.Events;

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
}

