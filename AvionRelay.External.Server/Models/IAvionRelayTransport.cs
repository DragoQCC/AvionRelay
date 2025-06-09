namespace AvionRelay.External.Server.Models;

public interface IAvionRelayTransport
{
    public TransportTypes SupportTransportType { get; }

    public Task RouteResponses(string senderID,Guid messageId, List<JsonResponse> responses);

    public Task RouteMessageToClient(string handlerId, TransportPackage package);
}