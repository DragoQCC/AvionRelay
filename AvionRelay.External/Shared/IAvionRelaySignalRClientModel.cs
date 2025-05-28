using AvionRelay.Core.Messages;

namespace AvionRelay.External;

/// <summary>
/// Invoked by the hub on the client, all argument values come from the hub
/// </summary>
public interface IAvionRelaySignalRClientModel
{
    public Task ReceivePackage(Package package);
    public Task<MessageResponse<TResponse>> SendResponse<TResponse>(Package package);
    public Task SendPackage(Package package);
    
}