using System.Text.Json;
using AvionRelay.External.Hub.Components.Connections;
using AvionRelay.External.Hub.Services;
using AvionRelay.External.Transports.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MessageReceiver = AvionRelay.Core.Dispatchers.MessageReceiver;
using Type = System.Type;

namespace AvionRelay.External.Hub.Features.Transports;

public class AvionRelayGrpcTransport : AvionRelayHub.AvionRelayHubBase
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly MessageHandlerTracker _handlerTracker;
    private readonly MessageStatistics _statistics;
    private readonly ResponseTracker _responseTracker;
    private readonly GrpcTransportMonitor _monitor;
    private readonly ILogger<AvionRelayGrpcTransport> _logger;
    
   
    
    public AvionRelayGrpcTransport(ConnectionTracker connectionTracker, MessageHandlerTracker handlerTracker, ResponseTracker responseTracker, GrpcTransportMonitor monitor, MessageStatistics statistics, ILogger<AvionRelayGrpcTransport> logger)
    {
        _connectionTracker = connectionTracker;
        _handlerTracker = handlerTracker;
        _responseTracker = responseTracker;
        _statistics = statistics;
        _monitor = monitor;
        _logger = logger;
    }
    
    public override async Task<ClientRegistrationResponse> RegisterClient(ClientRegistrationRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC client registration: {ClientId} - {ClientName}", request.ClientId, request.ClientName);
            
            // Convert metadata
            var metadata = request.Metadata.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object)kvp.Value);
            
            // Track the connection
            var hostAddress = new Uri($"grpc://{request.HostAddress}");
            _connectionTracker.TrackNewConnection(
                request.ClientId,
                request.ClientName,
                TransportTypes.Grpc,
                hostAddress,
                metadata);
            
            // Track transport mapping
            var connectionId = context.Peer ?? request.ClientId;
            _connectionTracker.TrackTransportToClientID(connectionId, request.ClientId);
            
            // Raise connection event
            await _monitor.RaiseClientConnected(new ClientConnectedEventCall
            {
                ClientId = request.ClientId,
                ClientName = request.ClientName,
                TransportType = TransportTypes.Grpc,
                HostAddress = hostAddress,
                ConnectedAt = DateTime.UtcNow,
                Metadata = metadata
            });
            
            // Register message handlers
            await _handlerTracker.AddMessageHandler(new MessageHandlerRegistration
            {
                HandlerID = request.ClientId,
                MessageNames = request.SupportedMessages.ToList()
            });
            
            return new ClientRegistrationResponse
            {
                Success = true,
                Message = "Registration successful",
                SessionId = Guid.NewGuid().ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering gRPC client");
            return new ClientRegistrationResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
    
    public override async Task<MessageResponseList> SendMessageWaitResponse(TransportPackageRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC message received: {MessageType} - {MessageId}", request.MessageTypeShortName, request.MessageId);
            
            // Convert to internal types
            var messageId = Guid.Parse(request.MessageId);
            var messageSize = System.Text.Encoding.UTF8.GetByteCount(request.MessageJson);
            _statistics.RecordMessageReceived(request.MessageTypeShortName,messageSize);
            
            
            
            // Get handlers for this message type
            var targetHandlerIds = _handlerTracker.GetMessageHandlers(request.MessageTypeShortName);
            _logger.LogDebug("Found {Count} handlers for {MessageType}",  targetHandlerIds.Count, request.MessageTypeShortName);
            
            if (targetHandlerIds.Count == 0)
            {
                return new MessageResponseList();
            }
            
            // Track pending response
            _responseTracker.TrackPendingResponse(
                messageId,
                request.SenderId,
                expectedResponseCount: targetHandlerIds.Count,
                timeout: TimeSpan.FromSeconds(30));
            
            // Forward to handlers (this would typically use the streaming connection)
            // For now, we'll store it for handlers to retrieve
            await ForwardToHandlers(request, targetHandlerIds);
            
            // Wait for responses
            var responses = await _responseTracker.WaitForResponsesAsync(messageId);
            
            // Convert to gRPC response format
            var grpcResponses = new MessageResponseList();
            foreach (var response in responses)
            {
                GrpcJsonResponse grpcResponse = new()
                {
                    MessageId = response.MessageId.ToString(),
                    Acknowledger = new GrpcMessageReceiver()
                    {
                        ReceiverId = response.Acknowledger.ReceiverId,
                        Name = response.Acknowledger.Name ?? ""
                    },
                    ResponseJson = response.ResponseJson

                };
                
                grpcResponses.Responses.Add(grpcResponse);
            }
            
            // Complete tracking
            _responseTracker.CompleteTracking(messageId);
            
            return grpcResponses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC message");
            return new MessageResponseList();
        }
    }
    
    public override async Task<ResponseAck> SendResponse(ResponseMessage request, ServerCallContext context)
    {
        try
        {
            var messageId = Guid.Parse(request.Response.MessageId);
            var connectionId = context.Peer ?? "";
            var clientId = _connectionTracker.GetClientIDFromTransportID(connectionId);
            
            _logger.LogInformation("gRPC response received for message {MessageId} from {ClientId}",messageId, clientId);
            
            
            // Record the response
            JsonResponse jsonResponse = new()
            {
                MessageId = messageId,
                Acknowledger = new MessageReceiver(),
                ResponseJson = request.Response.ResponseJson
            };
            var allResponsesReceived = _responseTracker.RecordResponse(messageId, clientId, jsonResponse);
            
            if (allResponsesReceived)
            {
                // All responses received - the SendMessageWaitResponse call will handle returning them
                _logger.LogInformation("All responses received for message {MessageId}", messageId);
            }
            
            return new ResponseAck
            {
                Success = true,
                Message = "Response recorded"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gRPC response");
            return new ResponseAck
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
    
    public override async Task MessageStream(IAsyncStreamReader<StreamMessage> requestStream,IServerStreamWriter<StreamMessage> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("gRPC streaming connection established from {Peer}", context.Peer);
        
        // Handle bidirectional streaming for real-time message delivery
        try
        {
            await foreach (var message in requestStream.ReadAllAsync(context.CancellationToken))
            {
                if (message.MessageCase == StreamMessage.MessageOneofCase.Response)
                {
                    // Handle response
                    await SendResponse(new ResponseMessage { Response = message.Response }, context);
                }
                else if (message.MessageCase == StreamMessage.MessageOneofCase.Heartbeat)
                {
                    // Echo heartbeat
                    await responseStream.WriteAsync(new StreamMessage 
                    { 
                        Heartbeat = "pong" 
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in gRPC message stream");
        }
        finally
        {
            _logger.LogInformation("gRPC streaming connection closed from {Peer}", context.Peer);
        }
    }
    
    public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request,ServerCallContext context)
    {
        return Task.FromResult(new HealthCheckResponse
        {
            Healthy = true,
            Status = "OK",
            ServerTime = Timestamp.FromDateTime(DateTime.UtcNow)
        });
    }
    
    private async Task ForwardToHandlers(TransportPackageRequest request, List<string> handlerIds)
    {
        // This is a simplified version - in production, you'd send through the streaming connections
        // For now, we'll just log it
        _logger.LogDebug("Would forward message {MessageId} to handlers: {Handlers}", request.MessageId, string.Join(", ", handlerIds));
        
        // In the full implementation, you'd maintain a registry of streaming connections
        // and forward the message through those connections
    }
}