using AvionRelay.External.Transports.Grpc;
using HelpfulTypesAndExtensions;
using HelpfulTypesAndExtensions.Errors;
using BaseMessageType = AvionRelay.Core.Messages.BaseMessageType;
using MessageContext = AvionRelay.Core.Messages.MessageContext;
using MessagePriority = AvionRelay.Core.Messages.MessagePriority;
using MessageState = AvionRelay.Core.Messages.MessageState;
using Acknowledgement = AvionRelay.Core.Messages.Acknowledgement;
using MessageReceiver = AvionRelay.Core.Dispatchers.MessageReceiver;

namespace AvionRelay.External.Server.Grpc;

public static class GrpcExtensions
{
    public static TransportPackage ToTransportPackage(this TransportPackageRequest packageRequest, MessageContext metadata)
    {
        return new TransportPackage
        {
            MessageId = metadata.MessageId,
            MessageTypeName = metadata.MessageTypeName,
            BaseMessageType = metadata.BaseMessageType,
            MessageJson = packageRequest.MessageJson,
            Priority = metadata.Priority,
            CreatedAt = new DateTime(metadata.CreatedAt.UtcTicks, DateTimeKind.Utc),
            SenderId = packageRequest.SenderId,
            HandlerIdsOrNames = packageRequest.HandlerNamesOrIds.ToList()
        };

    }

    public static MessagePriority ToMessagePriority(this AvionRelay.External.Transports.Grpc.MessagePriority messagePriority)
    {
        switch (messagePriority)
        {
            case AvionRelay.External.Transports.Grpc.MessagePriority.Low:
                return MessagePriority.Low;
            case AvionRelay.External.Transports.Grpc.MessagePriority.Normal:
                return MessagePriority.Normal;
            case AvionRelay.External.Transports.Grpc.MessagePriority.High:
                return MessagePriority.High;
            case AvionRelay.External.Transports.Grpc.MessagePriority.VeryHigh:
                return MessagePriority.VeryHigh;
            case AvionRelay.External.Transports.Grpc.MessagePriority.Critical:
                return MessagePriority.Critical;
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

    public static MessageState ToMessageState(this Transports.Grpc.MessageState messageState)
    {
        switch (messageState)
        {
            case Transports.Grpc.MessageState.Created:
                return MessageState.Created;
            case Transports.Grpc.MessageState.Sent: 
                return MessageState.Sent;
            case Transports.Grpc.MessageState.Received:
                return MessageState.Received;
            case Transports.Grpc.MessageState.Processing:
                return MessageState.Processing;
            case Transports.Grpc.MessageState.Responded:
                return MessageState.Responded;
            case Transports.Grpc.MessageState.ResponseReceived:
                return MessageState.ResponseReceived;
            case Transports.Grpc.MessageState.AcknowledgementReceived:
                return MessageState.AcknowledgementReceived;
            case Transports.Grpc.MessageState.Failed:
                return MessageState.Failed;
            default:
                throw new ArgumentOutOfRangeException(nameof(messageState), messageState, null);
        }
    }

    public static MessageReceiver ToMessageReceiver(this Transports.Grpc.MessageReceiver messageReceiver)
    {
        return new MessageReceiver(messageReceiver.ReceiverId, messageReceiver.Name);
    }
    

    public static MessageContext ToMessageContext(this AvionRelay.External.Transports.Grpc.MessageContext messageContext, AvionRelay.External.Transports.Grpc.MessageReceiver receiver, Google.Protobuf.WellKnownTypes.Timestamp handledAt)
    {
        var context = new MessageContext()
        {
            MessageId = Guid.Parse(messageContext.MessageId),
            CreatedAt = messageContext.CreatedAt.ToDateTime(),
            BaseMessageType = messageContext.BaseMessageType.ToBaseMessageType(),
            IsCancelled = messageContext.IsCancelled,
            Priority = messageContext.Priority.ToMessagePriority(),
            State = messageContext.State.ToMessageState(),
            RetryCount = messageContext.RetryCount,
            MessageTypeName = messageContext.MessageTypeName
        };

        foreach (var ack in messageContext.Acknowledgements)
        {
            context.Acknowledgements.Add(new Acknowledgement(context.MessageId, receiver.ToMessageReceiver(),handledAt.ToDateTime()));
        }
        return context;
    }

    public static Transports.Grpc.MessageState ToGrpcMessageState(this MessageState messageState)
    {
        return messageState switch
        {
            MessageState.Created => Transports.Grpc.MessageState.Created,
            MessageState.Sent => Transports.Grpc.MessageState.Sent,
            MessageState.Received => Transports.Grpc.MessageState.Received,
            MessageState.Processing => Transports.Grpc.MessageState.Processing,
            MessageState.Responded => Transports.Grpc.MessageState.Responded,
            MessageState.ResponseReceived => Transports.Grpc.MessageState.ResponseReceived,
            MessageState.AcknowledgementReceived => Transports.Grpc.MessageState.AcknowledgementReceived,
            MessageState.Failed => Transports.Grpc.MessageState.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(messageState), messageState, null)
        };
    }

    public static Transports.Grpc.BaseMessageType ToGrpcBaseMessageType(this BaseMessageType baseMessageType)
    {
        return baseMessageType switch
        {
            BaseMessageType.Command => Transports.Grpc.BaseMessageType.Command,
            BaseMessageType.Notification => Transports.Grpc.BaseMessageType.Notification,
            BaseMessageType.Alert => Transports.Grpc.BaseMessageType.Alert,
            BaseMessageType.Inspection => Transports.Grpc.BaseMessageType.Inspection,
            _ => throw new ArgumentOutOfRangeException(nameof(baseMessageType), baseMessageType, null)
        };
    }

    public static Transports.Grpc.MessagePriority ToGrpcMessagePriority(this MessagePriority messagePriority)
    {
        return messagePriority switch
        {
            MessagePriority.Low => Transports.Grpc.MessagePriority.Low,
            MessagePriority.Normal => Transports.Grpc.MessagePriority.Normal,
            MessagePriority.High => Transports.Grpc.MessagePriority.High,
            MessagePriority.VeryHigh => Transports.Grpc.MessagePriority.VeryHigh,
            MessagePriority.Critical => Transports.Grpc.MessagePriority.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(messagePriority), messagePriority, null)
        };
    }


    public static Transports.Grpc.MessageContext ToGrpcMessageContext(this MessageContext context)
    {
        return new Transports.Grpc.MessageContext()
        {
            MessageId = context.MessageId.ToString(),
            BaseMessageType = context.BaseMessageType.ToGrpcBaseMessageType(),
            State = context.State.ToGrpcMessageState(),
            Priority = context.Priority.ToGrpcMessagePriority(),
            RetryCount = context.RetryCount,
            IsCancelled = context.IsCancelled,
            Acknowledgements = {  }
        };
    }

    public static MessageErrorType ToMessageErrorType(this Transports.Grpc.MessageErrorType errorType)
    {
        return errorType switch
        {
            Transports.Grpc.MessageErrorType.NetworkError => MessageErrorType.NetworkError,
            Transports.Grpc.MessageErrorType.ServerError => MessageErrorType.ServerError,
            Transports.Grpc.MessageErrorType.ClientError => MessageErrorType.ClientError,
            Transports.Grpc.MessageErrorType.Other => MessageErrorType.Other,
            _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, null)
        };

    }

    public static MessagingError ToMessagingError(this Transports.Grpc.MessagingError messagingError)
    {
        return new MessagingError()
        {
            ErrorMessage = messagingError.ErrorMessage,
            ErrorType = messagingError.ErrorType.ToMessageErrorType(),
            ErrorPriority = messagingError.ErrorPriority.ToMessagePriority(),
            ErrorTimestamp = messagingError.ErrorTimestamp.ToDateTime(),
            Source = messagingError.Source,
            Suggestion = messagingError.Suggestion
        };
    }
    
}