using System.Collections.Concurrent;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using AvionRelay.External.Server.Models;
using HelpfulTypesAndExtensions;
using Metalama.Framework.Aspects;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.Services;

public class AvionRelayTransportRouter
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageHandlerTracker _handlerTracker;
    private readonly MessageStatistics _statistics;
    private readonly ResponseTracker _responseTracker;
    private readonly ITransportMonitor _monitor;
    private readonly JsonTransformService _jsonTransformService;
    private readonly AvionRelayExternalOptions _avionConfiguration;
    private readonly IMessageScheduler _messageScheduler;
    private readonly ILogger<AvionRelayTransportRouter> _logger;
    private readonly ConcurrentDictionary<TransportTypes, IAvionRelayTransport> _transports = new();

    /// <summary>
    /// Key is the client id or name, value is if resend is in progress currently
    /// </summary>
    private readonly ConcurrentDictionary<string,bool> _clientResendInProgress = new();

    public AvionRelayTransportRouter(
        ConnectionTracker connectionTracker, MessageHandlerTracker handlerTracker,
        ResponseTracker responseTracker, MessageStatistics statistics,
        ITransportMonitor monitor, JsonTransformService jsonTransformService,
        IMessageScheduler messageScheduler,
        AvionRelayExternalOptions avionConfiguration, ILogger<AvionRelayTransportRouter> logger
        )
    {
        _connectionTracker = connectionTracker;
        _handlerTracker = handlerTracker;
        _responseTracker = responseTracker;
        _statistics = statistics;
        _monitor = monitor;
        _jsonTransformService = jsonTransformService;
        _messageScheduler = messageScheduler;
        _avionConfiguration = avionConfiguration;
        _logger = logger;

        _responseTracker.MessageErrorSetEvent.Subscribe<MessageErrorSetEventCall>(TryResendMessage);
        _responseTracker.MessageResponseSetEvent.Subscribe<MessageResponseSetEventCall>(RouteResponse);
    }
    
    /// <summary>
    /// Should be called when a transport first starts up so messages can be routed correctly
    /// </summary>
    /// <param name="transport"></param>
    public void RegisterTransport(IAvionRelayTransport transport)
    {
        _transports[transport.TransportType] = transport;
        _logger.LogInformation("Registered transport: {TransportType}", transport.TransportType);
    }
    
    
    public void UnregisterTransport(TransportTypes transportType)
    {
        if (_transports.TryRemove(transportType, out var transport))
        {
            _logger.LogInformation("Unregistered transport: {TransportType}", transportType);
        }
    }

    /// <summary>
    /// Start tracking a new client connection to one of the transports
    /// </summary>
    /// <param name="clientRegistration"></param>
    /// <param name="transportId"></param>
    public async Task<ClientRegistrationResponse> TrackNewTransportClient(ClientRegistrationRequest clientRegistration, string transportId)
    {
        try
        {
            Guid clientId = Guid.CreateVersion7();
            string clientIdString = clientId.ToString();
        
            _connectionTracker.TrackNewConnection(clientIdString, transportId, clientRegistration.ClientName, clientRegistration.TransportType, clientRegistration.HostAddress, clientRegistration.Metadata);
        
            await _monitor.RaiseClientConnected(new ClientConnectedEventCall()
            {
                ClientId = clientIdString,
                ClientName = clientRegistration.ClientName,
                TransportType = clientRegistration.TransportType,
                HostAddress = clientRegistration.HostAddress,
                Metadata = clientRegistration.Metadata
            });
        
            MessageHandlerRegistration messagesForHandler = new MessageHandlerRegistration()
            {
                HandlerID = clientIdString,
                MessageNames = clientRegistration.SupportedMessages
            };
            await _handlerTracker.AddMessageHandler(messagesForHandler);

            return new ClientRegistrationResponse()
            {
                ClientId = clientId,
                Success = true,
                FailureMessage = null,
                ServerVersion = "1.0"
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Failed to register client, error message: {errorMessage}", e.Message);
            return new ClientRegistrationResponse()
            {
                ClientId = Guid.Empty,
                Success = false,
                FailureMessage = null,
                ServerVersion = "1.0"
            };
        }
    }
    
    /// <summary>
    /// Gets the handlers, and transport method and forwards the message over the assigned transport
    /// </summary>
    /// <param name="transportPackage"></param>
    /// <param name="metadata"></param>
    public async Task ForwardToHandlers(TransportPackage transportPackage, MessageContext metadata)
    {
        try
        {
            _logger.LogDebug("Starting to track message");
            _responseTracker.TrackPendingResponse(transportPackage.MessageId, transportPackage.SenderId, transportPackage.HandlerIdsOrNames.Count);
            _logger.LogDebug("Getting Message Receivers");
            List<MessageReceiver> receivers = await GetMessageReceiversForPackage(transportPackage);
            foreach (MessageReceiver receiver in receivers)
            {
                _responseTracker.AddHandlerForTrackedMessage(transportPackage.MessageId, receiver);
            }
            
            _logger.LogDebug("Getting active client connections");
            List<ClientConnection> connections = await GetHandlerConnections(transportPackage);
            if (connections.Any())
            {
                _logger.LogDebug("Forwarding message {MessageId} to handlers: {Handlers}", metadata.MessageId, string.Join(", ", connections.Select(x => x.ClientId)));
                _logger.LogDebug("Routing to active receivers");
                await RouteToTargets(transportPackage, connections);
            }
        }
        catch (Exception e)
        {
            //TODO: I may want to put this logic into a foreach so each individual receiver can propagate an error if needed
            MessagingError error = new MessagingError()
            {
                ErrorMessage = e.Message,
                Source = _avionConfiguration.ApplicationName,
                ErrorPriority = MessagePriority.Critical,
                ErrorTimestamp = DateTime.UtcNow,
                ErrorType = MessageErrorType.ServerError
            };
            await _responseTracker.SetMessagingErrorFor(transportPackage, new MessageReceiver("",""), error);
        }
    }

    
    public async Task HandleResponseForMessage(Guid messageID, ResponsePayload response)
    {
        try
        {
            _logger.LogInformation("Received response for message {MessageID}",messageID.ToString());
            _responseTracker.RecordResponse(messageID,response);
            await _responseTracker.MessageResponseSetEvent.AlertForMessageResponseReceived(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Failed to send response");
        }
    }
    
    
    private async Task TryResendMessage(MessageErrorSetEventCall messageFailure)
    {
        string nameOrId = messageFailure.targetClient.Receiver.ReceiverId.IsEmpty()
            ? messageFailure.targetClient.Receiver.Name
            : messageFailure.targetClient.Receiver.ReceiverId;

        _clientResendInProgress.TryAdd(nameOrId, false);
        
        _logger.LogDebug("Checking if message has exhausted resend attempts for this client {nameOrId}",nameOrId);
        Guid messageId = messageFailure.transportPackage.MessageId;

        while (_clientResendInProgress[nameOrId])
        {
            await Task.Delay(100);
        }
        
        TimeSpan? retryWaitingPeriod = _messageScheduler.ShouldRetryDelivery(messageFailure.transportPackage.Priority, ref messageFailure.targetClient.FailureCount);
        if (retryWaitingPeriod is not null)
        {
            _clientResendInProgress[nameOrId] = true;
            _logger.LogDebug("Waiting {waitingPeriod} to resend message {messageId}",retryWaitingPeriod, messageId);
            _logger.LogDebug("Waiting started at {DateTime.UtcNow} to resend message", DateTime.UtcNow);
            await Task.Delay(retryWaitingPeriod.Value);
            _logger.LogDebug("Waiting ended at {DateTime.UtcNow} to resend message", DateTime.UtcNow);
            ClientConnection? connection = _connectionTracker.FilterConnectionsForTargetClient(await GetHandlerConnections(messageFailure.transportPackage), nameOrId);
            if (connection is not null)
            {
                if (_handlerTracker.IsClientHandler(messageFailure.transportPackage.MessageTypeName, connection.ClientId))
                {
                    _logger.LogDebug("Resending message to client {ClientName}", messageFailure.targetClient.Receiver.Name);
                    //TODO: Once the client re-connected this sent a message per failure?
                    await RouteToTargets(messageFailure.transportPackage, [connection]);
                }
                else
                {
                    _logger.LogDebug("Client connection found but client is not set to handle this message type");
                }
            }
            else
            {
                _logger.LogDebug("Failed to find connection for requested client {NameOrId}", nameOrId);
            }
        }
        else
        {
            _logger.LogWarning("Failed to send message {MessageId} to {TargetClientId} within allowed retry limit, returning error as result", messageId, nameOrId);
            if (messageFailure.transportPackage.BaseMessageType is BaseMessageType.Alert or BaseMessageType.Notification)
            {
                return;
            }
            await HandleResponseForMessage(messageId, messageFailure.targetClient.Response);
        }
        _clientResendInProgress[nameOrId] = false;
    }


    
    private async Task RouteResponse(MessageResponseSetEventCall eventCall)
    {
        try
        {
            ResponsePayload response = eventCall.Response;
            string senderConnectionId = _responseTracker.GetSenderConnectionId(eventCall.Response.MessageId);
            ClientConnection? connection = _connectionTracker.GetConnection(senderConnectionId);
            if (connection is  null)
            {
                throw new Exception($"No connection tracked for sender {senderConnectionId}");
            }
            //this waits until all responses come back or a timeout happens
            //var allResponses = await _responseTracker.WaitForResponsesAsync(eventCall.Response.MessageId);

            IAvionRelayTransport? transportToUse = null;
            string? transportId = null;

            if (connection.TransportType is TransportTypes.SignalR)
            {
                //this will be the SignalR connection ID for the client that orginally sent the message for processing and is awaiting a response
                transportId = _connectionTracker.GetTransportIDFromClientID(senderConnectionId);
                _transports.TryGetValue(TransportTypes.SignalR, out transportToUse);
            }
            else if (connection.TransportType is TransportTypes.Grpc)
            {
                //grpc does not use an internal ID system to track clients like SignalR so we can just send the senderConnectionID
                transportId = connection.ClientId;
                _transports.TryGetValue(TransportTypes.Grpc, out transportToUse);
            }

            if (transportToUse is null)
            {
                _logger.LogWarning("Could not find transport for requested type {TransportType}", connection.TransportType.ToString());
            }

            if (transportId is null)
            {
                _logger.LogWarning("Could not find transport ID for Client ID {ClientId}", connection.ClientId);
            }
            _logger.LogInformation("Sending response back to sender {senderID}", transportId);

            //TODO: Im getting back responses again but now I need the client side to be updated to take 1+ at a time instead of thinking the list it gets is everything
            var updatedresponses = _jsonTransformService.TransformResponsesForClient(connection.ClientId, [response]);
            bool isFinalResponse = _responseTracker.GotAllResponsesForMessage(response.MessageId);
            await transportToUse.RouteResponses(transportId, updatedresponses, isFinalResponse);
            _responseTracker.MessageCleanupReady(response.MessageId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task<List<ClientConnection>> GetHandlerConnections(TransportPackage transportPackage)
    {
        try
        {
            List<string> idsToCheck = [ ];
            //These message types broadcast to all handlers vs. specifying a target
            if (transportPackage.BaseMessageType is BaseMessageType.Notification or BaseMessageType.Alert)
            {
                idsToCheck = _handlerTracker.GetMessageHandlers(transportPackage.MessageTypeName);
            }
            else
            {
                idsToCheck = transportPackage.HandlerIdsOrNames;
            }
            
            var registeredHandlerIds = _handlerTracker.GetMessageHandlers(transportPackage.MessageTypeName);
            registeredHandlerIds.Remove(transportPackage.SenderId);

            _logger.LogDebug("Found {HandlerCount}, handlers registered for this message type: {MessageName}", registeredHandlerIds.Count, transportPackage.MessageTypeName);

            List<ClientConnection> connections = [ ];
            //Parse the list of targets to get client IDs from the send IDs or Names 
            foreach (string nameOrId in idsToCheck)
            {
                MessageReceiver? receiver = _connectionTracker.GetMessageReceiver(nameOrId);
                if (receiver is null)
                {
                    _logger.LogDebug("Unknown connection ID / Friendly Name: {NameOrId}", nameOrId);
                    
                    var messagingError = new MessagingError(
                        "Unknown connection ID / Friendly Name: " + nameOrId, 
                        _avionConfiguration.ApplicationName, 
                        MessageErrorType.ServerError, 
                        MessagePriority.High, 
                        DateTime.UtcNow,
                        "Verify Client ID or Friendly name value used is correct, and that client is connected to the server"
                    );
                    await  _responseTracker.SetMessagingErrorFor(transportPackage, new MessageReceiver("",nameOrId), messagingError);
                    //if this errors we want to move to the next ID
                    continue;
                }
                //Check if the requested handler is set as a handler for this message
                if (registeredHandlerIds.Contains(receiver.ReceiverId) is false)
                {
                    _logger.LogDebug("Client {HandlerId} is not registered to handle message type", receiver.ReceiverId);
                    var messagingError = new MessagingError(
                        $"Client {nameOrId} is not registered to handle this message type", 
                        _avionConfiguration.ApplicationName, 
                        MessageErrorType.ServerError, 
                        MessagePriority.High, 
                        DateTime.UtcNow,
                        "Ensure this client registered to handle this message type"
                    );
                    await _responseTracker.SetMessagingErrorFor(transportPackage, receiver, messagingError);
                    continue;
                }
                
                ClientConnection? connection = _connectionTracker.GetConnection(receiver.ReceiverId);
                if (connection is not null)
                {
                    connections.Add(connection);
                }
                else
                {
                    var messagingError = new MessagingError()
                    {
                        ErrorMessage = $"Client has connected previously however no active connection found for desired target: ID:{receiver.ReceiverId} Name:{receiver.Name}",
                        Source = _avionConfiguration.ApplicationName,
                        ErrorPriority = MessagePriority.VeryHigh,
                        ErrorTimestamp = DateTime.UtcNow,
                        ErrorType = MessageErrorType.ServerError,
                        Suggestion = "Ensure target client is running and connected prior to messaging"
                    };
                    await _responseTracker.SetMessagingErrorFor(transportPackage, receiver, messagingError);
                }
            }
            return connections;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<List<MessageReceiver>> GetMessageReceiversForPackage(TransportPackage transportPackage)
    {
        List<string> idsToCheck = [ ];
        //These message types broadcast to all handlers vs. specifying a target
        if (transportPackage.BaseMessageType is BaseMessageType.Notification or BaseMessageType.Alert)
        {
            idsToCheck = _handlerTracker.GetMessageHandlers(transportPackage.MessageTypeName);
        }
        else
        {
            idsToCheck = transportPackage.HandlerIdsOrNames;
        }
        
        List<MessageReceiver> receivers = [];
        foreach (string nameOrId in idsToCheck)
        {
            MessageReceiver? messageReceiver = _connectionTracker.GetMessageReceiver(nameOrId);
            if (messageReceiver is not null)
            {
                receivers.Add(messageReceiver);
            }
            //TODO: Do I need to make fake receivers here so errors for invalid entries can be tracked and sent back still?
        }
        return receivers;
    }

    
    private async Task RouteToTargets(TransportPackage package, List<ClientConnection> connections)
    {
        //Get the transport and forward it to the client/handler
        IAvionRelayTransport? transportToUse = null;
        foreach (var connection in connections)
        {
            string? transportId = null;
            if (connection.TransportType == TransportTypes.SignalR)
            {
                transportId = _connectionTracker.GetTransportIDFromClientID(connection.ClientId);
                _transports.TryGetValue(TransportTypes.SignalR, out transportToUse);
            }
            else if (connection.TransportType == TransportTypes.Grpc)
            {
                transportId = connection.ClientId;
                _transports.TryGetValue(TransportTypes.Grpc, out transportToUse);
            }

            if (transportToUse is not null && transportId is not null)
            {
                var updatedPackage = _jsonTransformService.TransformPackageForClient(connection.ClientId, package);
                await transportToUse.RouteMessageToClient(transportId, updatedPackage);
            }
        }
    }
    
}