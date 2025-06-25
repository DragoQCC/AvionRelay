using System.Text;
using AvionRelay.Core.Messages;
using AvionRelay.External.Server.Models;
using AvionRelay.External.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.SignalR;

// Adapter that implements IAvionRelayTransport for SignalR
public class SignalRTransportAdapter : IAvionRelayTransport
{
    private readonly IHubContext<AvionRelaySignalRTransport, IAvionRelaySignalRClientModel> _hubContext;
    private readonly ILogger<SignalRTransportAdapter> _logger;
    private readonly AvionRelayTransportRouter _transportRouter;
    
    public TransportTypes TransportType => TransportTypes.SignalR;
    
    public SignalRTransportAdapter(IHubContext<AvionRelaySignalRTransport, IAvionRelaySignalRClientModel> hubContext, AvionRelayTransportRouter transportRouter, ILogger<SignalRTransportAdapter> logger)
    {
        _hubContext = hubContext;
        _transportRouter = transportRouter;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RouteResponses(string senderID, List<ResponsePayload> responses, bool isFinalResponse)
    {
        await _hubContext.Clients.Client(senderID).ReceiveResponses(responses, isFinalResponse);
    }

    /// <inheritdoc />
    public async Task RouteMessageToClient(string handlerId, TransportPackage package)
    {
        await _hubContext.Clients.Client(handlerId).ReceivePackage(package);
    }
}


/// <summary>
/// The actual SignalR Hub that clients connect to
/// </summary>
public class AvionRelaySignalRTransport : Hub<IAvionRelaySignalRClientModel>, IAvionRelaySignalRHubModel
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageStatistics _statistics;
    private readonly SignalRTransportMonitor _monitor;
    private readonly MessageHandlerTracker _handlerTracker;
    private readonly ResponseTracker _responseTracker;
    private readonly ILogger<AvionRelaySignalRTransport> _logger;
    private readonly AvionRelayTransportRouter _transportRouter;

    public AvionRelaySignalRTransport(
        ConnectionTracker connectionTracker, MessageStatistics statistics, SignalRTransportMonitor monitor, 
        MessageHandlerTracker handlerTracker, ResponseTracker responseTracker, AvionRelayTransportRouter transportRouter, ILogger<AvionRelaySignalRTransport> logger)
    {
        _connectionTracker = connectionTracker;
        _statistics = statistics;
        _monitor = monitor;
        _handlerTracker = handlerTracker;
        _responseTracker = responseTracker;
        _transportRouter = transportRouter;
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("OnConnectedAsync called");
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string avionClientID =  _connectionTracker.GetClientIDFromTransportID(Context.ConnectionId);
        _connectionTracker.StopTrackingConnection(avionClientID);
        _handlerTracker.RemoveHandler(avionClientID);
        await _monitor.RaiseClientDisconnected(new ClientDisconnectedEventCall()
        {
            ClientId = avionClientID,
            DisconnectedAt = DateTime.UtcNow,
            Reason = exception?.Message
        });
        
        await base.OnDisconnectedAsync(exception);
    }
    
    

    public async Task SendMessage(TransportPackage package)
    {
        try
        {
            Console.WriteLine("Received message send request");
            //get the size of the message.Package.Message in bytes
            int messageSize = Encoding.UTF8.GetByteCount(package.MessageJson);
            var metadata = JsonExtensions.TryGetJsonSubsectionAs<MessageContext>(package.MessageJson, "metadata", new(){PropertyNameCaseInsensitive = true});
        
            _logger.LogInformation("Message Info: Name:{Name}, ID:{MessageID} ", package.MessageTypeName, package.MessageId);
            
            _statistics.RecordMessageReceived(package.MessageTypeName,messageSize);
            await _transportRouter.ForwardToHandlers(package,metadata);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <inheritdoc />
    public async Task SendMessageWaitResponse(TransportPackage package)
    {
        try
        {
            _logger.LogInformation("Executing message wait response");
            //get the size of the message.Package.Message in bytes
            int messageSize = Encoding.UTF8.GetByteCount(package.MessageJson);

            var metadata = JsonExtensions.TryGetJsonSubsectionAs<MessageContext>(package.MessageJson, "metadata", new(){PropertyNameCaseInsensitive = true});
        
            _logger.LogInformation("Message Info: Name:{Name}, ID:{MessageID} ", package.MessageTypeName, package.MessageId);
            
            _statistics.RecordMessageReceived(package.MessageTypeName,messageSize);
            await _transportRouter.ForwardToHandlers(package,metadata);
        }
        catch (Exception e)
        {
            _logger.LogError(e,"SignalR hub failed to forward message");
        }
    }

    /// <inheritdoc />
    public async Task SendResponse(ResponsePayload response)
    {
        string messageId = response.MessageId.ToString();
        _logger.LogInformation("Received response for message {MessageID}",messageId);
        await _transportRouter.HandleResponseForMessage(response.MessageId,response);
    }

    
    /// <inheritdoc />
    public async Task<ClientRegistrationResponse> RegisterClient(ClientRegistrationRequest clientRegistration)
    {
        return await _transportRouter.TrackNewTransportClient(clientRegistration, Context.ConnectionId);
    }
}