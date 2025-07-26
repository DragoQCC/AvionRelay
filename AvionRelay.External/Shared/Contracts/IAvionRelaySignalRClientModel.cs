namespace AvionRelay.External;

/// <summary>
/// Invoked by the hub on the client, all argument values come from the hub
/// </summary>
public interface IAvionRelaySignalRClientModel
{
    public Task ReceivePackage(TransportPackage transportPackage);
    public Task ReceiveResponses(List<ResponsePayload> responses, bool isFinalResponse = false);
    

}