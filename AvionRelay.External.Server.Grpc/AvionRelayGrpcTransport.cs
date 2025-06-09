using System.Collections.Concurrent;
using AvionRelay.External.Server.Models;
using AvionRelay.External.Server.Services;
using AvionRelay.External.Transports.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Acknowledgement = AvionRelay.External.Transports.Grpc.Acknowledgement;
using MessageContext = AvionRelay.Core.Messages.MessageContext;

namespace AvionRelay.External.Server.Grpc;

public class AvionRelayGrpcTransport : AvionRelayHub.AvionRelayHubBase, IAvionRelayTransport
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageHandlerTracker _handlerTracker;
    private readonly MessageStatistics _statistics;
    private readonly ResponseTracker _responseTracker;
    private readonly GrpcTransportMonitor _monitor;
    private readonly ILogger<AvionRelayGrpcTransport> _logger;
    private readonly AvionRelayTransportRouter _transportRouter;
    
    // Track active client streams
    private readonly ConcurrentDictionary<string, ClientStreamContext> _activeStreams = new();
    
    /// <inheritdoc />
    public TransportTypes SupportTransportType => TransportTypes.Grpc;

    

    public AvionRelayGrpcTransport(ConnectionTracker connectionTracker, MessageHandlerTracker handlerTracker, 
        ResponseTracker responseTracker, GrpcTransportMonitor monitor, 
        MessageStatistics statistics, AvionRelayTransportRouter transportRouter, 
        ILogger<AvionRelayGrpcTransport> logger)
    {
        _connectionTracker = connectionTracker;
        _handlerTracker = handlerTracker;
        _responseTracker = responseTracker;
        _statistics = statistics;
        _transportRouter = transportRouter;
        _monitor = monitor;
        _logger = logger;
    }
    
    public override async Task Communicate(IAsyncStreamReader<ClientMessage> requestStream, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
    {
        ClientStreamContext? streamContext = null;
        try
        {
            _logger.LogInformation("gRPC client connecting from {Peer}", context.Peer);
            
            // Read messages from client
            await foreach (var clientMessage in requestStream.ReadAllAsync(context.CancellationToken))
            {
                switch (clientMessage.MessageCase)
                {
                    case ClientMessage.MessageOneofCase.Registration:
                        streamContext = await HandleRegistration(clientMessage.Registration, responseStream, context);
                        break;
                        
                    case ClientMessage.MessageOneofCase.SendMessage:
                        await HandleSendMessage(clientMessage.SendMessage, streamContext);
                        break;
                        
                    case ClientMessage.MessageOneofCase.SendMessageWaitResponse:
                        await HandleSendMessageWaitResponse(clientMessage.SendMessageWaitResponse, streamContext);
                        break;
                        
                    case ClientMessage.MessageOneofCase.SendResponse:
                        await HandleSendResponse(clientMessage.SendResponse, streamContext);
                        break;
                        
                    case ClientMessage.MessageOneofCase.Acknowledge:
                        await HandleAcknowledge(clientMessage.Acknowledge, streamContext);
                        break;
                        
                    case ClientMessage.MessageOneofCase.Heartbeat:
                        await HandleHeartbeat(clientMessage.Heartbeat, responseStream);
                        break;
                        
                    case ClientMessage.MessageOneofCase.StatusUpdate:
                        await HandleStatusUpdate(clientMessage.StatusUpdate, streamContext);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC stream for client {ClientId}", 
                streamContext?.ClientId ?? "unknown");
        }
        finally
        {
            // Clean up on disconnect
            if (streamContext != null)
            {
                await HandleDisconnect(streamContext);
            }
        }
    }
    
    /// <inheritdoc />
    public async Task RouteResponses(string senderID, Guid messageId, List<JsonResponse> responses)
    {
        // Convert to gRPC response format
        var grpcResponses = new MessageResponseList();
        grpcResponses.Responses.Add(ConvertResponsesToGrpc(responses));
        
        //write the responses
        if (_activeStreams.TryGetValue(senderID, out var senderStream))
        {
            await senderStream.ResponseStream.WriteAsync(new ServerMessage
            {
                ReceiveResponses = grpcResponses
            });
        }
        else
        {
            _logger.LogWarning("Grpc stream not being tracked for client {ClientID}", senderID);
        }
    }

    /// <inheritdoc />
    public async Task RouteMessageToClient(string handlerId, TransportPackage package)
    {
        await SendMessageToClient(handlerId, package.ToTransportPackageRequest());
    }

    private async Task<ClientStreamContext> HandleRegistration(ClientRegistrationRequest request, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
    {
        try
        {
            string clientId = Guid.CreateVersion7().ToString();
            var connectionId = context.Peer;
            _logger.LogInformation("gRPC client registration: {ClientId} - {ClientName}", clientId, request.ClientName);
            
            // Create stream context
            var streamContext = new ClientStreamContext
            {
                ClientId = clientId,
                ClientName = request.ClientName,
                ResponseStream = responseStream,
                ServerCallContext = context,
                ConnectedAt = DateTime.UtcNow
            };
            
            // Store the active stream
            _activeStreams[clientId] = streamContext;
            
            // Convert metadata
            var metadata = request.Metadata.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object)kvp.Value);
            
            
            // Track the connection
            var hostAddress = new Uri($"grpc://{context.Peer.Replace("ipv4:","")}");

            ClientRegistration clientReg = new ClientRegistration()
            {
                ClientId = clientId,
                ClientName = request.ClientName,
                HostAddress = hostAddress,
                Metadata = metadata,
                TransportType = SupportTransportType,
            };

            await _transportRouter.TrackNewTransportClient(clientReg, connectionId);
            
            /*_connectionTracker.TrackNewConnection(
                clientId,
                connectionId,
                request.ClientName,
                TransportTypes.Grpc,
                hostAddress,
                metadata);
            
            // Track transport mapping
            _connectionTracker.TrackTransportToClientID(connectionId, clientId);
            
            // Raise connection event
            await _monitor.RaiseClientConnected(new ClientConnectedEventCall
            {
                ClientId = clientId,
                ClientName = request.ClientName,
                TransportType = TransportTypes.Grpc,
                HostAddress = hostAddress,
                ConnectedAt = DateTime.UtcNow,
                Metadata = metadata
            });
            
            // Register message handlers
            await _handlerTracker.AddMessageHandler(new MessageHandlerRegistration
            {
                HandlerID = clientId,
                MessageNames = request.SupportedMessages.ToList()
            });*/
            
            // Send registration response
            await responseStream.WriteAsync(new ServerMessage
            {
                RegistrationResponse = new ClientRegistrationResponse
                {
                    Success = true,
                    ClientId = clientId
                }
            });
            
            return streamContext;
        }
        catch (Exception ex)
        {
            await responseStream.WriteAsync(new ServerMessage
            {
                RegistrationResponse = new ClientRegistrationResponse
                {
                    Success = false,
                    FailureMessage = ex.Message
                }
            });
            throw;
        }
    }
    
    private async Task HandleSendMessage(TransportPackageRequest request, ClientStreamContext? streamContext)
    {
        if (streamContext == null)
        {
            _logger.LogWarning("Received message from unregistered client");
            return;
        }
        
        //pull out just the metadata from the request
        var messageMetadata = Server.JsonExtensions.GetMessageContextFromJson(request.MessageJson);
        var messageId = messageMetadata.MessageId;
        _logger.LogInformation("gRPC message received: {MessageType} - {MessageId}", request.MessageTypeShortName, messageId);
        
        var messageSize = System.Text.Encoding.UTF8.GetByteCount(request.MessageJson);
        _statistics.RecordMessageReceived(request.MessageTypeShortName, messageSize);
        
        // Get handlers for this message type
        var targetHandlerIds = _handlerTracker.GetMessageHandlers(request.MessageTypeShortName);
        _logger.LogDebug("Found {Count} handlers for {MessageType}",  targetHandlerIds.Count, request.MessageTypeShortName);
            
        if (targetHandlerIds.Count == 0)
        {
            return;
        }
        
        // Forward to handlers
        TransportPackage transportPackage = request.ToTransportPackage(messageMetadata);
        await _transportRouter.ForwardToHandlers(transportPackage, messageMetadata);
    }
    
    private async Task HandleSendMessageWaitResponse(TransportPackageRequest request, ClientStreamContext? streamContext)
    {
        try
        {
            if (streamContext == null)
            {
                _logger.LogWarning("Received message from unregistered client");
                return;
            }
            
            //pull out just the metadata from the request
            //var messageMetadata = Server.JsonExtensions.GetMessageContextFromJson(request.MessageJson);
            //TODO: if this works update the other instances
            var messageMetadata = JsonExtensions.TryGetJsonSubsectionAs<MessageContext>(request.MessageJson, "metadata", new(){PropertyNameCaseInsensitive = true});
            
            var messageId = messageMetadata.MessageId;
            _logger.LogInformation("gRPC message received: {MessageType} - {MessageId}", request.MessageTypeShortName, messageId);
            
            
            var messageSize = System.Text.Encoding.UTF8.GetByteCount(request.MessageJson);
            _statistics.RecordMessageReceived(request.MessageTypeShortName,messageSize);
            
            /*// Get handlers for this message type
            var targetHandlerIds = _handlerTracker.GetMessageHandlers(request.MessageTypeShortName);
            _logger.LogDebug("Found {Count} handlers for {MessageType}",  targetHandlerIds.Count, request.MessageTypeShortName);
            
            if (targetHandlerIds.Count == 0)
            {
                return;
            }
            
            // Track pending response
            _responseTracker.TrackPendingResponse(
                messageId,
                request.SenderId,
                expectedResponseCount: targetHandlerIds.Count,
                timeout: TimeSpan.FromSeconds(30));
                */
            
            // Forward to handlers
            TransportPackage transportPackage = request.ToTransportPackage(messageMetadata);
            await _transportRouter.ForwardToHandlers(transportPackage, messageMetadata);
            
            /*
            //TODO: See if I should remove this section or if I should write these responses to the stream from this method
            // Wait for responses
            var responses = await _responseTracker.WaitForResponsesAsync(messageId);

            // Convert to gRPC response format
            var grpcResponses = new MessageResponseList();
            grpcResponses.Responses.Add(ConvertResponsesToGrpc(responses));


            //write the responses
            // Get the original sender and send all responses
            var originalSenderId = _responseTracker.GetSenderConnectionId(messageId);
            if (originalSenderId == null)
            {
                _logger.LogError("Unable to find ID of message sender for message {MessageId}", messageId);
                return;
            }
            if (_activeStreams.TryGetValue(originalSenderId, out var senderStream))
            {
                await senderStream.ResponseStream.WriteAsync(new ServerMessage
                {
                    ReceiveResponses = grpcResponses
                });
                _responseTracker.CompleteTracking(messageId);
            }
            else
            {
                _logger.LogWarning("Grpc stream not being tracked for client {ClientID}", originalSenderId);
            }*/
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC message");
        }
    }
    
    private async Task HandleSendResponse(ResponseMessage request, ClientStreamContext? context)
    {
        try
        {
            var messageId = Guid.Parse(request.MessageId);
            var clientId = request.Acknowledger.ReceiverId;
            
            _logger.LogInformation("gRPC response received for message {MessageId} from {ClientId}",messageId, clientId);
            
            
            // Record the response
            JsonResponse jsonResponse = new()
            {
                MessageId = messageId,
                Acknowledger = new AvionRelay.Core.Dispatchers.MessageReceiver()
                {
                    ReceiverId = clientId,
                    Name = request.Acknowledger.Name,
                },
                ResponseJson = request.ResponseJson
            };

            await _transportRouter.SendResponseForMessage(jsonResponse);
            
            /*var allResponsesReceived = _responseTracker.RecordResponse(jsonResponse);
            if (allResponsesReceived)
            {
                // All responses received - the SendMessageWaitResponse call will handle returning them
                _logger.LogInformation("All responses received for message {MessageId}", messageId);
                {
                    // Get the original sender and send all responses
                    var originalSenderId = _responseTracker.GetSenderConnectionId(messageId);
                    var allResponses = await _responseTracker.WaitForResponsesAsync(messageId);
                    if (originalSenderId != null && _activeStreams.TryGetValue(originalSenderId, out var senderStream))
                    {
                        await senderStream.ResponseStream.WriteAsync(new ServerMessage
                        {
                            ReceiveResponses = new MessageResponseList
                            {
                                Responses = { ConvertResponsesToGrpc(allResponses) },
                            }
                        });
                
                        _responseTracker.CompleteTracking(messageId);
                    }
                    //TODO: Sender could be signalR so need to check for that and call the hub context instead if it is not a grpc sender
                    string originalSendertransportId = _connectionTracker.GetTransportIDFromClientID(originalSenderId);
                    await _hubContext.Clients.Client(originalSendertransportId).ReceiveResponses(messageId, allResponses);
                }
            }*/
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC response");
        }
    }
    
    private async Task HandleAcknowledge(Acknowledgement ack, ClientStreamContext? streamContext)
    {
        if (streamContext == null)
        {
            return;
        }

        _logger.LogDebug("Message {MessageId} acknowledged by {ClientId}", ack.MessageId, streamContext.ClientId);
        
        //TODO: Process acknowledgment
        // You might want to track acknowledgments similar to responses
    }
    
    private async Task HandleHeartbeat(Heartbeat heartbeat, IServerStreamWriter<ServerMessage> responseStream)
    {
        // Echo heartbeat back
        await responseStream.WriteAsync(new ServerMessage
        {
            Heartbeat = new Heartbeat
            {
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                SequenceNumber = heartbeat.SequenceNumber
            }
        });
    }
    
    private async Task HandleStatusUpdate(ClientStatusUpdate statusUpdate, ClientStreamContext? streamContext)
    {
        if (streamContext == null)
        {
            return;
        }

        _logger.LogInformation("Client {ClientId} status: {Status}", 
                               streamContext.ClientId, statusUpdate.Status);
        
        // TODO:Update client status in tracking
        // You might want to store this information for monitoring
    }
    
    private async Task HandleDisconnect(ClientStreamContext streamContext)
    {
        _logger.LogInformation("gRPC client {ClientId} disconnecting", streamContext.ClientId);
        
        // Remove from active streams
        _activeStreams.TryRemove(streamContext.ClientId, out _);
        
        // Stop tracking connection
        _connectionTracker.StopTrackingConnection(streamContext.ClientId);
        
        // Raise disconnection event
        await _monitor.RaiseClientDisconnected(new ClientDisconnectedEventCall
        {
            ClientId = streamContext.ClientId,
            DisconnectedAt = DateTime.UtcNow,
            Reason = "Stream closed"
        });
    }
    
    /*private async Task ForwardToHandlers(TransportPackageRequest request, MessageContext metadata, List<string> handlerIds)
    {
        // This is a simplified version - in production, you'd send through the streaming connections
        // For now, we'll just log it
        _logger.LogDebug("Would forward message {MessageId} to handlers: {Handlers}", metadata.MessageId, string.Join(", ", handlerIds));

        List<ClientConnection> connections = new();
        foreach (string handlerId in handlerIds)
        {
            var connection = _connectionTracker.GetConnection(handlerId);
            connections.Add(connection);
        }

        foreach (var connection in connections)
        {
            var transportId = _connectionTracker.GetTransportIDFromClientID(connection.ClientId);
            if (connection.TransportType == TransportTypes.SignalR)
            {
                await _hubContext.Clients.Client(transportId).ReceivePackage(request.ToTransportPackage(metadata));
            }
        }

    }*/
    
    private IEnumerable<ResponseMessage> ConvertResponsesToGrpc(List<JsonResponse> responses)
    {
        return responses.Select(r => new ResponseMessage
        {
            MessageId = r.MessageId.ToString(),
            Acknowledger = new MessageReceiver
            {
                ReceiverId = r.Acknowledger.ReceiverId,
                Name = r.Acknowledger.Name ?? ""
            },
            ResponseJson = GrpcJsonTransformer.TransformForGrpcClient(r.ResponseJson),
            RespondedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        });
    }
    

    private async Task SendMessageToClient(string clientId, TransportPackageRequest message)
    {
        if (_activeStreams.TryGetValue(clientId, out var streamContext))
        {
            _logger.LogDebug("Sending message to client ID {ClientId}",clientId);
            // Transform the message JSON for gRPC client
            var transformedMessage = new TransportPackageRequest
            {
                SenderId = message.SenderId,
                MessageTypeShortName = message.MessageTypeShortName,
                MessageJson = GrpcJsonTransformer.TransformForGrpcClient(message.MessageJson)
            };

            await streamContext.ResponseStream.WriteAsync(
                new ServerMessage
                {
                    ReceiveMessage = transformedMessage
                }
            );
        }
        else
        {
            _logger.LogWarning("Cannot send message to client {ClientId} - not connected", clientId);
        }
    }


}