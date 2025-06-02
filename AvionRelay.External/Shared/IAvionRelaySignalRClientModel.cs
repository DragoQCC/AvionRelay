using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;

namespace AvionRelay.External;

/// <summary>
/// Invoked by the hub on the client, all argument values come from the hub
/// </summary>
public interface IAvionRelaySignalRClientModel
{
    public Task ReceivePackage(TransportPackage package);
    public Task SendPackage(Package package);
    public Task ReceiveResponses(Guid messageId,List<MessageResponse<object>> responses);
    

}