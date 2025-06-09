using AvionRelay.Core.Messages;
using AvionRelay.External.Server.Models;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.Services;

public class AvionRelayTransportRouter
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageHandlerTracker _handlerTracker;
    private readonly MessageStatistics _statistics;
    private readonly ResponseTracker _responseTracker;
    private readonly ITransportMonitor _monitor;
    private readonly ILogger<AvionRelayTransportRouter> _logger;
    private readonly IEnumerable<IAvionRelayTransport> _transports;


    public AvionRelayTransportRouter(
        ConnectionTracker connectionTracker, MessageHandlerTracker handlerTracker,
        ResponseTracker responseTracker, MessageStatistics statistics,
        IEnumerable<IAvionRelayTransport> transports, ITransportMonitor monitor,
        ILogger<AvionRelayTransportRouter> logger
        )
    {
        _connectionTracker = connectionTracker;
        _handlerTracker = handlerTracker;
        _responseTracker = responseTracker;
        _statistics = statistics;
        _transports = transports;
        _monitor = monitor;
        _logger = logger;
    }

    /// <summary>
    /// Start tracking a new client connection to one of the transports
    /// </summary>
    /// <param name="clientRegistration"></param>
    /// <param name="transportId"></param>
    public async Task TrackNewTransportClient(ClientRegistration clientRegistration, string transportId)
    {
        _connectionTracker.TrackNewConnection(clientRegistration.ClientId, transportId, clientRegistration.ClientName, clientRegistration.TransportType, clientRegistration.HostAddress, clientRegistration.Metadata);
        _connectionTracker.TrackTransportToClientID(transportId, clientRegistration.ClientId);
        
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
    
    public async Task ForwardToHandlers(TransportPackage package, MessageContext metadata)
    {
        var handlerIds = _handlerTracker.GetMessageHandlers(package.MessageTypeShortName);
        
        Console.WriteLine($"Found {handlerIds.Count} handlers");
        // Track this as a pending response
        _responseTracker.TrackPendingResponse(
            package.MessageId,
            package.SenderId,
            expectedResponseCount: handlerIds.Count,
            timeout: TimeSpan.FromSeconds(30)
        );
        
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

        IAvionRelayTransport? transportToUse = null;
        foreach (var connection in connections)
        {
            string? transportId = null;
            if (connection.TransportType == TransportTypes.SignalR)
            {
                transportId = _connectionTracker.GetTransportIDFromClientID(connection.ClientId);
                transportToUse = _transports.First(x => x.SupportTransportType is TransportTypes.SignalR);
            }
            else if (connection.TransportType == TransportTypes.Grpc)
            {
                transportId = connection.ClientId;
                transportToUse = _transports.First(x => x.SupportTransportType is TransportTypes.Grpc);
            }
            if (transportToUse is not null && transportId is not null)
            {
                await transportToUse.RouteMessageToClient(transportId, package);
            }
        }
    }

    public async Task SendResponseForMessage(JsonResponse response)
    {
        try
        {
            _logger.LogInformation("Received response for message {MessageID}",response.MessageId.ToString());
            
            bool allResponsesReceived = _responseTracker.RecordResponse(response);
            string? senderConnectionId = _responseTracker.GetSenderConnectionId(response.MessageId);

            if (senderConnectionId is null)
            {
                _logger.LogDebug("Could not get sender for message id {MessageId}", response.MessageId.ToString());
                return;
            }
        
            ClientConnection? connection = _connectionTracker.GetConnection(senderConnectionId);
            if (connection is  null)
            {
                throw new Exception($"No connection tracked for sender {senderConnectionId}");
            }
            var allResponses = await _responseTracker.WaitForResponsesAsync(response.MessageId);
            
            IAvionRelayTransport? transportToUse = null;
            string? transportId = null;
            
            if (connection.TransportType is TransportTypes.SignalR)
            {
                //this will be the SignalR connection ID for the client that orginally sent the message for processing and is awaiting a response
                transportId = _connectionTracker.GetTransportIDFromClientID(senderConnectionId);
                transportToUse = _transports.First(x => x.SupportTransportType is TransportTypes.SignalR);
            }
            else if (connection.TransportType is TransportTypes.Grpc)
            {
                //grpc does not use an internal ID system to track clients like SignalR so we can just send the senderConnectionID
                transportId = connection.ClientId;
                transportToUse = _transports.First(x => x.SupportTransportType is TransportTypes.Grpc);
            }
            
            if (transportToUse is not null && transportId is not null)
            {
                _logger.LogInformation("Sending response back to sender {senderID}", transportId);
                await transportToUse.RouteResponses(transportId, response.MessageId, allResponses);
                _responseTracker.CompleteTracking(response.MessageId);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Failed to send response");
        }
    }
    
    
}