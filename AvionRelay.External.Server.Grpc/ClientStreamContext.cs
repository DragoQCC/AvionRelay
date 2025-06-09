using AvionRelay.External.Transports.Grpc;
using Grpc.Core;

namespace AvionRelay.External.Server.Grpc;

// Context class to track client stream information
public class ClientStreamContext
{
    public required string ClientId { get; init; }
    public required string ClientName { get; init; }
    public required IServerStreamWriter<ServerMessage> ResponseStream { get; init; }
    public required ServerCallContext ServerCallContext { get; init; }
    public DateTime ConnectedAt { get; init; }
}