using System.Text;
using AvionRelay.Core;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using AvionRelay.External.Hub.Components.Connections;
using AvionRelay.External.Hub.Services;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace AvionRelay.External.Hub.Features.Transports;

/// <summary>
/// The actual SignalR Hub that clients connect to
/// </summary>
public partial class AvionRelaySignalRHub : Hub<IAvionRelaySignalRClientModel>, IAvionRelaySignalRHubModel
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageStatistics _statistics;
    private readonly SignalRTransportMonitor _monitor;
    private readonly MessageHandlerTracker _handlerTracker;
    private readonly ResponseTracker _responseTracker;
    private readonly ILogger<AvionRelaySignalRHub> _logger;

    
    
    public AvionRelaySignalRHub(ConnectionTracker connectionTracker, MessageStatistics statistics, SignalRTransportMonitor monitor, MessageHandlerTracker handlerTracker, ResponseTracker responseTracker, ILogger<AvionRelaySignalRHub> logger)
    {
        _connectionTracker = connectionTracker;
        _statistics = statistics;
        _monitor = monitor;
        _handlerTracker = handlerTracker;
        _responseTracker = responseTracker;
        _logger = logger;
    }
    
    public override async Task OnConnectedAsync()
    {
        //todo: ILogger cannot be created or injected into the hub so a different work around will be needed :/
        _logger.LogInformation("OnConnectedAsync called");
        //Console.WriteLine("Client connected: " + Context.ConnectionId);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionTracker.StopTrackingConnection(Context.ConnectionId);
        
        await _monitor.RaiseClientDisconnected(new ClientDisconnectedEventCall()
        {
            ClientId = Context.ConnectionId,
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
        
            _logger.LogInformation("Message Info: Name:{Name}, ID:{MessageID} ", package.MessageTypeShortName, package.MessageId);
            
            _statistics.RecordMessageReceived(package.MessageTypeShortName,messageSize);

            var targetHandlerIds = _handlerTracker.GetMessageHandlers(package.MessageTypeShortName);
            
            Console.WriteLine($"Found {targetHandlerIds.Count} handlers");
        
            // Send to all handlers
            Console.WriteLine("Calling ReceivePackage on clients");
            List<string> signalRClientIds = new();
            foreach (string handlerId in targetHandlerIds)
            {
                signalRClientIds.Add(_connectionTracker.GetTransportIDFromClientID(handlerId));
            }
            await Clients.Clients(signalRClientIds).ReceivePackage(package);
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
            Console.WriteLine("Received message send request");
            //get the size of the message.Package.Message in bytes
            int messageSize = Encoding.UTF8.GetByteCount(package.MessageJson);
        
            _logger.LogInformation("Message Info: Name:{Name}, ID:{MessageID} ", package.MessageTypeShortName, package.MessageId);
            
            _statistics.RecordMessageReceived(package.MessageTypeShortName,messageSize);
        
            /*await _monitor.RaiseMessageReceived(new MessageReceivedEventCall()
            {
                Package = message.Package as Package,
                FromClientId = message.SenderId,
                MessageSize = 100
            });*/

            var targetHandlerIds = _handlerTracker.GetMessageHandlers(package.MessageTypeShortName);
            
            Console.WriteLine($"Found {targetHandlerIds.Count} handlers");

            // Track this as a pending response
            _responseTracker.TrackPendingResponse(
                package.MessageId,
                package.SenderId,
                expectedResponseCount: targetHandlerIds.Count,
                timeout: TimeSpan.FromSeconds(30)
            );
        
            // Send to all handlers
            Console.WriteLine("Calling ReceivePackage on clients");
            List<string> signalRClientIds = new();
            foreach (string handlerId in targetHandlerIds)
            {
                signalRClientIds.Add(_connectionTracker.GetTransportIDFromClientID(handlerId));
            }
            await Clients.Clients(signalRClientIds).ReceivePackage(package);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    /// <inheritdoc />
    public async Task SendResponse(Guid messageId, JsonResponse response)
    {
        _logger.LogInformation("Received response for message {MessageID}",messageId);
        var messengerID = _connectionTracker.GetClientIDFromTransportID(Context.ConnectionId);
        var allResponsesReceived = _responseTracker.RecordResponse(messageId, messengerID, response);
        string originalSenderID = _responseTracker.GetSenderConnectionId(messageId);
        
        //this will be the SignalR connection ID for the client that orginally sent the message for processing and is awaiting a response
        var senderConnectionId = _connectionTracker.GetTransportIDFromClientID(originalSenderID);
        if (senderConnectionId != null)
        {
            _logger.LogInformation("Sending response back to sender {senderID}", senderConnectionId);
            // If this was the last expected response, send all responses back to the original sender
            var allResponses = await _responseTracker.WaitForResponsesAsync(messageId);
            await Clients.Client(senderConnectionId).ReceiveResponses(messageId, allResponses);
        }
    }

    
    /// <inheritdoc />
    public async Task RegisterClient(ClientRegistration clientRegistration)
    {
        _connectionTracker.TrackNewConnection(clientRegistration.ClientId, clientRegistration.ClientName, clientRegistration.TransportType, clientRegistration.HostAddress, clientRegistration.Metadata);
        
        //This ensures the SignalR client value can also be found from just the client ID
        _connectionTracker.TrackTransportToClientID(Context.ConnectionId, clientRegistration.ClientId);
        
        await _monitor.RaiseClientConnected(new ClientConnectedEventCall()
        {
            ClientId = clientRegistration.ClientId,
            ClientName = clientRegistration.ClientName,
            TransportType = clientRegistration.TransportType,
            HostAddress = clientRegistration.HostAddress,
            Metadata = clientRegistration.Metadata
        });
        
        MessageHandlerRegistration messagesForHandler = new MessageHandlerRegistration()
        {
            HandlerID = clientRegistration.ClientId,
            MessageNames = clientRegistration.SupportedMessages
        };
        
        await _handlerTracker.AddMessageHandler(messagesForHandler);
    }
}

//This portion of the partial class will hold all the methods that the hub can invoke on clients
public partial class AvionRelaySignalRHub
{
    
}