using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;

namespace AvionRelay.External.Transports.SignalR;

/// <summary>
/// A class to register to the hub connection to handle .On calls from the SignalR hub
/// </summary>
public class SignalROnHandler : IAvionRelaySignalRClientModel
{
    public MessageResponseReceivedEvent MessageResponseReceivedEvent = new();
    
    /// <inheritdoc />
    public async Task ReceivePackage(TransportPackage transportPackage)
    {
        try
        {
            Package? package = transportPackage.ToPackage();
            if (package != null)
            {
                await MessageHandlerRegister.ProcessPackage(package);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <inheritdoc />
    public async Task ReceiveResponses(List<ResponsePayload> responses, bool isFinalResponse = false)
    {
        await MessageResponseReceivedEvent.NotifyResponseReceived(responses,isFinalResponse);
    }
}