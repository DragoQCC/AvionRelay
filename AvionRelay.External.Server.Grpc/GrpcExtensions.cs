using AvionRelay.External.Transports.Grpc;
using BaseMessageType = AvionRelay.Core.Messages.BaseMessageType;
using MessageContext = AvionRelay.Core.Messages.MessageContext;
using MessagePriority = AvionRelay.Core.Messages.MessagePriority;

namespace AvionRelay.External.Server.Grpc;

public static class GrpcExtensions
{
    public static TransportPackage ToTransportPackage(this TransportPackageRequest packageRequest, MessageContext metadata)
    {
        return new TransportPackage
        {
            MessageId = metadata.MessageId,
            MessageTypeShortName = packageRequest.MessageTypeShortName,
            BaseMessageType = metadata.BaseMessageType,
            MessageJson = packageRequest.MessageJson,
            Priority = metadata.Priority,
            CreatedAt = new DateTime(metadata.CreatedAt.UtcTicks, DateTimeKind.Utc),
            SenderId = packageRequest.SenderId 
        };

    }

    public static MessagePriority ToMessagePriority(this AvionRelay.External.Transports.Grpc.MessagePriority messagePriority)
    {
        switch (messagePriority)
        {
            case AvionRelay.External.Transports.Grpc.MessagePriority.Lowest:
                return MessagePriority.Lowest;
            case AvionRelay.External.Transports.Grpc.MessagePriority.Low:
                return MessagePriority.Low;
            case AvionRelay.External.Transports.Grpc.MessagePriority.Normal:
                return MessagePriority.Normal;
            case AvionRelay.External.Transports.Grpc.MessagePriority.High:
                return MessagePriority.High;
            case AvionRelay.External.Transports.Grpc.MessagePriority.VeryHigh:
                return MessagePriority.VeryHigh;
            case AvionRelay.External.Transports.Grpc.MessagePriority.Highest:
                return MessagePriority.Highest;
            default:
                throw new ArgumentOutOfRangeException(nameof(messagePriority), messagePriority, null);
        }
    }

    public static BaseMessageType ToBaseMessageType(this AvionRelay.External.Transports.Grpc.BaseMessageType baseMessageType)
    {
        switch (baseMessageType)
        {
            case AvionRelay.External.Transports.Grpc.BaseMessageType.Command:
                return BaseMessageType.Command;
            case AvionRelay.External.Transports.Grpc.BaseMessageType.Notification:
                return BaseMessageType.Notification;
            case AvionRelay.External.Transports.Grpc.BaseMessageType.Alert:
                return BaseMessageType.Alert;
            case AvionRelay.External.Transports.Grpc.BaseMessageType.Inspection:
                return BaseMessageType.Inspection;
            default:
                throw new ArgumentOutOfRangeException(nameof(baseMessageType), baseMessageType, null);
        }
    }
    
}