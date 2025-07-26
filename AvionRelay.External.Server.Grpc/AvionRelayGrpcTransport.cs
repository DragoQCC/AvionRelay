using System.Collections.Concurrent;
using AvionRelay.External.Server.Models;
using AvionRelay.External.Server.Services;
using AvionRelay.External.Transports.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
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
    public TransportTypes TransportType => TransportTypes.Grpc;

    

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
        
        _transportRouter.RegisterTransport(this);
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
    public async Task RouteResponses(string senderID, List<ResponsePayload> responses, bool isFinalResponse = false)
    {
        // Convert to gRPC response format
        var grpcResponses = new MessageResponseList();
        grpcResponses.Responses.Add(ConvertResponsesToGrpc(responses));
        
        //write the responses
        if (_activeStreams.TryGetValue(senderID, out var senderStream))
        {
            //TODO: Should implement a dead letter caller here as protection in cases where this fails
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

    private async Task<ClientStreamContext> HandleRegistration(AvionRelay.External.Transports.Grpc.ClientRegistrationRequest request, IServerStreamWriter<ServerMessage> responseStream, ServerCallContext context)
    {
        try
        {
            var connectionId = context.Peer;
            
            // Convert metadata
            var metadata = request.Metadata.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object)kvp.Value);
            
            // Track the connection
            var hostAddress = new Uri($"grpc://{context.Peer.Replace("ipv4:","")}");
            
            AvionRelay.External.ClientRegistrationRequest clientRegReq = new ClientRegistrationRequest()
            {
                ClientName = request.ClientName,
                HostAddress = hostAddress,
                TransportType = TransportTypes.Grpc,
                ClientVersion = request.ClientVersion,
                Metadata = metadata,
                SupportedMessages = request.SupportedMessages.ToList()
            };
            
            AvionRelay.External.ClientRegistrationResponse registrationResponse =  await _transportRouter.TrackNewTransportClient(clientRegReq, connectionId);
            if (registrationResponse.Success is false)
            {
                throw new Exception(registrationResponse.FailureMessage);
            }
            
            // Create stream context
            var streamContext = new ClientStreamContext
            {
                ClientId = registrationResponse.ClientId.ToString(),
                ClientName = request.ClientName,
                ResponseStream = responseStream,
                ServerCallContext = context,
                ConnectedAt = DateTime.UtcNow
            };
            
            // Store the active stream
            _activeStreams[registrationResponse.ClientId.ToString()] = streamContext;
            
            _logger.LogInformation("gRPC client registration: {ClientId} - {ClientName}", registrationResponse.ClientId.ToString(), request.ClientName);
            
            // Send registration response
            await responseStream.WriteAsync(new ServerMessage
            {
                RegistrationResponse = new AvionRelay.External.Transports.Grpc.ClientRegistrationResponse
                {
                    Success = true,
                    ClientId = registrationResponse.ClientId.ToString()
                }
            });
            
            return streamContext;
        }
        catch (Exception ex)
        {
            await responseStream.WriteAsync(new ServerMessage
            {
                RegistrationResponse = new AvionRelay.External.Transports.Grpc.ClientRegistrationResponse
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
        var messageMetadata = JsonExtensions.TryGetJsonSubsectionAs<MessageContext>(request.MessageJson, "metadata", new(){PropertyNameCaseInsensitive = true});
        var messageId = messageMetadata.MessageId;
        _logger.LogInformation("gRPC message received: {MessageType} - {MessageId}", messageMetadata.MessageTypeName, messageId);
        var messageSize = System.Text.Encoding.UTF8.GetByteCount(request.MessageJson);
        _statistics.RecordMessageReceived(messageMetadata.MessageTypeName, messageSize);
        
        // Forward to handlers
        TransportPackage transportPackage = request.ToTransportPackage(messageMetadata);
        _transportRouter.ForwardToHandlers(transportPackage, messageMetadata);
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
            var messageMetadata = JsonExtensions.TryGetJsonSubsectionAs<MessageContext>(request.MessageJson, "metadata", new(){PropertyNameCaseInsensitive = true});
            
            var messageId = messageMetadata.MessageId;
            _logger.LogDebug("gRPC message received: {MessageType} - {MessageId}", messageMetadata.MessageTypeName, messageId);
            
            var messageSize = System.Text.Encoding.UTF8.GetByteCount(request.MessageJson);
            _statistics.RecordMessageReceived(messageMetadata.MessageTypeName,messageSize);
            
            // Forward to handlers
            TransportPackage transportPackage = request.ToTransportPackage(messageMetadata);
            _transportRouter.ForwardToHandlers(transportPackage, messageMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC message");
        }
    }
    
    private async Task HandleSendResponse(MessageResponse request, ClientStreamContext? context)
    {
        try
        {
            var messageId = Guid.Parse(request.MessageId);
            //TODO: a caller could forget to supply values so adding a check and returning an error would be good
            var clientId = request.Receiver.ReceiverId;
            
            _logger.LogDebug("gRPC response received for message {MessageId} from {ClientId}",messageId, clientId);
            
            ResponsePayload? response = null;
            Core.Dispatchers.MessageReceiver convertedReceiver = request.Receiver.ToMessageReceiver();
            if (request.ResponseCase == MessageResponse.ResponseOneofCase.Acknowledgement)
            {
                response = new ResponsePayload(messageId,convertedReceiver, request.RespondedAt.ToDateTime());
            }
            else if (request.ResponseCase == MessageResponse.ResponseOneofCase.MessagingError)
            {
                response = new ResponsePayload(messageId,request.MessagingError.ToMessagingError(), convertedReceiver);
            }
            else
            {
                response = new ResponsePayload(messageId,convertedReceiver, request.RespondedAt.ToDateTime(), request.ResponseJson);
            }
            
            await _transportRouter.HandleResponseForMessage(messageId, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC response");
        }
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

        _logger.LogInformation("Client {ClientId} status: {Status}",  streamContext.ClientId, statusUpdate.Status);
        
        // TODO:Update client status in tracking
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
    
    private IEnumerable<MessageResponse> ConvertResponsesToGrpc(List<ResponsePayload> responses)
    {
        return responses.Select(r => new MessageResponse
        {
            MessageId = r.MessageId.ToString(),
            Receiver = new MessageReceiver
            {
                ReceiverId = r.Receiver.ReceiverId,
                Name = r.Receiver.Name ?? ""
            },
            ResponseJson = (r.ResponseJson is not null) ? GrpcJsonTransformer.TransformForGrpcClient(r.ResponseJson) : null,
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