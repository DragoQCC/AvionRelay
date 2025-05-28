using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;

namespace AvionRelay.External;

public interface IAvionRelaySignalRHubModel
{
    public Task SendPackage(Package package);
    public Task<bool> RegisterReceiver(ExternalMessageReceiver receiver);

    Task RegisterClient(ClientRegistration clientRegistration);
}