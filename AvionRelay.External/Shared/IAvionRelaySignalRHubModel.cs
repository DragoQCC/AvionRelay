using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;

namespace AvionRelay.External;

public interface IAvionRelaySignalRHubModel
{
    public Task SendMessage(TransportPackage package);
    
    /// <summary>
    /// Won't be able to return the responses but informs the client and hub that the additional methods to store and send the responses should happen
    /// </summary>
    /// <param name="package"></param>
    /// <returns></returns>
    public Task SendMessageWaitResponse(TransportPackage package);

    Task RegisterClient(ClientRegistration clientRegistration);

    public Task SendResponse(Guid messageId, JsonResponse response);
}