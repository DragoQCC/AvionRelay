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
    public async Task ReceivePackage(TransportPackage package)
    {
        try
        {
            Package convertedPackage = package.ToPackage();
            await MessageHandlerRegister.ProcessPackage(convertedPackage);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    

    /// <inheritdoc />
    public async Task SendPackage(Package package)
    {
    }

    /// <inheritdoc />
    public async Task ReceiveResponses(Guid messageId, List<MessageResponse<object>> responses)
    {
        await MessageResponseReceivedEvent.NotifyResponseReceived(messageId, responses);
    }
}