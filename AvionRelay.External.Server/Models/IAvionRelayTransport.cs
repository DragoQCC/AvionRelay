namespace AvionRelay.External.Server.Models;

public interface IAvionRelayTransport
{
    public TransportTypes TransportType { get; }

    public Task RouteResponses(string senderID, List<ResponsePayload> responses, bool isFinalResponse);

    public Task RouteMessageToClient(string handlerId, TransportPackage package);
}