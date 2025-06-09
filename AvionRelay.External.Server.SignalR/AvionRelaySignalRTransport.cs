using System.Text;
using AvionRelay.External.Server.Models;
using AvionRelay.External.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.SignalR;

/// <summary>
/// The actual SignalR Hub that clients connect to
/// </summary>
public partial class AvionRelaySignalRTransport : Hub<IAvionRelaySignalRClientModel>, IAvionRelaySignalRHubModel, IAvionRelayTransport
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageStatistics _statistics;
    private readonly SignalRTransportMonitor _monitor;
    private readonly MessageHandlerTracker _handlerTracker;
    private readonly ResponseTracker _responseTracker;
    private readonly ILogger<AvionRelaySignalRTransport> _logger;
    private readonly AvionRelayTransportRouter _transportRouter;

    /// <inheritdoc />
    public TransportTypes SupportTransportType => TransportTypes.SignalR;

   


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
        _connectionTracker.StopTrackingConnection(Context.ConnectionId);
        
        await _monitor.RaiseClientDisconnected(new ClientDisconnectedEventCall()
        {
            ClientId = Context.ConnectionId,
            DisconnectedAt = DateTime.UtcNow,
            Reason = exception?.Message
        });
        
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <inheritdoc />
    public async Task RouteResponses(string senderID, Guid messageId, List<JsonResponse> responses)
    {
        await Clients.Client(senderID).ReceiveResponses(messageId, responses);
    }

    /// <inheritdoc />
    public async Task RouteMessageToClient(string handlerId, TransportPackage package)
    {
        await Clients.Client(handlerId).ReceivePackage(package);
    }

    public async Task SendMessage(TransportPackage package)
    {
        try
        {
            Console.WriteLine("Received message send request");
            //get the size of the message.Package.Message in bytes
            int messageSize = Encoding.UTF8.GetByteCount(package.MessageJson);
            var metadata = JsonExtensions.GetMessageContextFromJson(package.MessageJson);
        
            _logger.LogInformation("Message Info: Name:{Name}, ID:{MessageID} ", package.MessageTypeShortName, package.MessageId);
            
            _statistics.RecordMessageReceived(package.MessageTypeShortName,messageSize);
            await _transportRouter.ForwardToHandlers(package,metadata);
            
            /*var targetHandlerIds = _handlerTracker.GetMessageHandlers(package.MessageTypeShortName);
            
            Console.WriteLine($"Found {targetHandlerIds.Count} handlers");
        
            // Send to all handlers
            Console.WriteLine("Calling ReceivePackage on clients");
            List<string> signalRClientIds = new();
            foreach (string handlerId in targetHandlerIds)
            {
                signalRClientIds.Add(_connectionTracker.GetTransportIDFromClientID(handlerId));
            }
            await Clients.Clients(signalRClientIds).ReceivePackage(package);*/
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

           var metadata = JsonExtensions.GetMessageContextFromJson(package.MessageJson);
        
            _logger.LogInformation("Message Info: Name:{Name}, ID:{MessageID} ", package.MessageTypeShortName, package.MessageId);
            
            _statistics.RecordMessageReceived(package.MessageTypeShortName,messageSize);
            await _transportRouter.ForwardToHandlers(package,metadata);
            /*await _monitor.RaiseMessageReceived(new MessageReceivedEventCall()
            {
                Package = message.Package as Package,
                FromClientId = message.SenderId,
                MessageSize = 100
            });*/

            //var targetHandlerIds = _handlerTracker.GetMessageHandlers(package.MessageTypeShortName);
            
            // Send to all handlers
           
            /*Console.WriteLine("Calling ReceivePackage on clients");
            List<string> signalRClientIds = new();
            foreach (string handlerId in targetHandlerIds)
            {
                signalRClientIds.Add(_connectionTracker.GetTransportIDFromClientID(handlerId));
            }
            await Clients.Clients(signalRClientIds).ReceivePackage(package);*/
        }
        catch (Exception e)
        {
            _logger.LogError(e,"SignalR hub failed to forward message");
        }
    }

    /// <inheritdoc />
    public async Task SendResponse(Guid messageId, JsonResponse response)
    {
        _logger.LogInformation("Received response for message {MessageID}",messageId);
        await _transportRouter.SendResponseForMessage(response);
        
        /*
        var messengerID = _connectionTracker.GetClientIDFromTransportID(Context.ConnectionId);
        var allResponsesReceived = _responseTracker.RecordResponse(messageId, messengerID, response);
        string? originalSenderID = _responseTracker.GetSenderConnectionId(messageId);

        if (originalSenderID is null)
        {
            _logger.LogDebug("Could not get sender for connection id {ConnectionID}", messengerID);
            return;
        }
        
        ClientConnection? connection = _connectionTracker.GetConnection(originalSenderID);
        //If this is not originally from a SignalR sender we exit after recording the response
        if (connection?.TransportType is not TransportTypes.SignalR)
        {
            return;
        }
        
        //this will be the SignalR connection ID for the client that orginally sent the message for processing and is awaiting a response
        var senderConnectionId = _connectionTracker.GetTransportIDFromClientID(originalSenderID);
        _logger.LogInformation("Sending response back to sender {senderID}", senderConnectionId);
        // If this was the last expected response, send all responses back to the original sender
        var allResponses = await _responseTracker.WaitForResponsesAsync(messageId);
        await Clients.Client(senderConnectionId).ReceiveResponses(messageId, allResponses);
        */
    }

    
    /// <inheritdoc />
    public async Task RegisterClient(ClientRegistration clientRegistration)
    {
        await _transportRouter.TrackNewTransportClient(clientRegistration, Context.ConnectionId);
        
        /*_connectionTracker.TrackNewConnection(clientRegistration.ClientId, Context.ConnectionId, clientRegistration.ClientName, TransportTypes.SignalR, clientRegistration.HostAddress, clientRegistration.Metadata);
        
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
        
        await _handlerTracker.AddMessageHandler(messagesForHandler);*/
    }
    
    /*private async Task ForwardToHandlers(TransportPackage package, MessageContext metadata, List<string> handlerIds)
    {
        // This is a simplified version - in production, you'd send through the streaming connections
        // For now, we'll just log it
        _logger.LogDebug("Forwarding message {MessageId} to handlers: {Handlers}", metadata.MessageId, string.Join(", ", handlerIds));

        List<ClientConnection> connections = new();
        foreach (string handlerId in handlerIds)
        {
            var connection = _connectionTracker.GetConnection(handlerId);
            if (connection is not null)
            {
                connections.Add(connection);
            }
        }

        foreach (var connection in connections)
        {
            if (connection.TransportType == TransportTypes.SignalR)
            {
                var transportId = _connectionTracker.GetTransportIDFromClientID(connection.ClientId);
                await Clients.Client(transportId).ReceivePackage(package);
            }

            if (connection.TransportType == TransportTypes.Grpc)
            {
                await _grpcTransport.SendMessageToClient(connection.ClientId, package.ToTransportPackageRequest());
            }
        }
    }*/
}

//This portion of the partial class will hold all the methods that the hub can invoke on clients
public partial class AvionRelaySignalRTransport
{
    
}