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

    Task<ClientRegistrationResponse> RegisterClient(ClientRegistrationRequest clientRegistration);

    public Task SendResponse(ResponsePayload response);
    
    public Task ReactivateClient(AvionRelayClient client, ClientRegistrationRequest registrationRequest);
}