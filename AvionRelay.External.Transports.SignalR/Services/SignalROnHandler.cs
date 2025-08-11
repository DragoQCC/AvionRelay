using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using Serilog;

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
            Log.Logger.Error(e, "Error encountered {ErrorMessage}", e.Message);
        }
    }

    /// <inheritdoc />
    public async Task ReceiveResponses(List<ResponsePayload> responses, bool isFinalResponse = false)
    {
        await MessageResponseReceivedEvent.NotifyResponseReceived(responses,isFinalResponse);
    }
}