using AvionRelay.External.Transports.Grpc;

namespace AvionRelay.External.Server.Grpc;

public static class MessageTypeHelpers
{
    public static TransportPackageRequest ToTransportPackageRequest(this TransportPackage package)
    {
        return new TransportPackageRequest
        {
            SenderId = package.SenderId,
            MessageJson = package.MessageJson,
        };
    }
    
}