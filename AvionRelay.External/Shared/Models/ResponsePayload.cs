using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using HelpfulTypesAndExtensions;

namespace AvionRelay.External;


public record MessagingError
{
    public required string Source { get; set; }
    public required string ErrorMessage { get; set; }
    public required MessageErrorType ErrorType { get; set; }
    public required MessagePriority ErrorPriority { get; set; }
    public required DateTime ErrorTimestamp { get; set; }
    public string? Suggestion { get; set; }

    [SetsRequiredMembers]
    public MessagingError(string error, string source, MessageErrorType errorType, MessagePriority errorPriority, DateTime errorTimestamp, string? suggestion = null)
    {
        ErrorMessage = error;
        Source = source;
        ErrorType = errorType;
        ErrorPriority = errorPriority;
        ErrorTimestamp = errorTimestamp;
        Suggestion = suggestion;
    }

    public MessagingError()
    {
        
    }
    
}

public enum MessageErrorType
{
    NetworkError = 0,
    ServerError = 1,
    ClientError = 2,
    Other = 3
}

public sealed class ResponsePayload
{
    public Guid MessageId { get; set; }
    public MessageReceiver Receiver { get; set; }
    public string? ResponseJson { get; set; }
    public MessagingError? Error { get; set; }
    public DateTime? HandledAt { get; set; }
    public MessageState ResponseState { get; set; }


    [JsonIgnore]
    public bool HasError => Error != null;
    
    /// <summary>
    /// True if the ResponseJson is not null, empty, or whitespace <br/>
    /// False if the ResponseJson is null, empty, or whitespace<br/>
    /// </summary>
    [JsonIgnore]
    public bool HasResponse => ResponseJson.HasValue();

    [JsonIgnore]
    public bool WasAcknowledged => ResponseState == MessageState.AcknowledgementReceived;
    
    public ResponsePayload(Guid messageId, MessageReceiver receiver, DateTime? handledAt)
    {
        MessageId = messageId;
        Receiver = receiver;
        HandledAt = handledAt;
        ResponseState = MessageState.AcknowledgementReceived;
    }
    
    public ResponsePayload(Guid messageId, MessageReceiver receiver, DateTime? handledAt, string responseJson)
    {
        MessageId = messageId;
        Receiver = receiver;
        HandledAt = handledAt;
        ResponseJson = responseJson;
        ResponseState = MessageState.ResponseReceived;
    }
    
    public ResponsePayload(Guid messageId, MessagingError error, MessageReceiver receiver)
    {
        MessageId = messageId;
        Error = error;
        Receiver = receiver;
        ResponseState = MessageState.Failed;
    }

    [JsonConstructor]
    private ResponsePayload()
    {
    }
}



public sealed class ResponsePayload<TResponse>
{
    public Guid MessageId { get; set; }
    public MessageReceiver Receiver { get; set; }
    public DateTime? HandledAt { get; set; }
    public MessageState ResponseState { get; set; }
    public MessagingError? Error { get; set; }
    
    public TResponse? Response { get; set; }
    
    [JsonIgnore]
    public bool HasResponse => Response != null;
    [JsonIgnore]
    public bool WasAcknowledged => ResponseState == MessageState.AcknowledgementReceived;
    [JsonIgnore]
    public bool HasError => Error != null;

    public ResponsePayload(Guid messageId, MessageReceiver receiver, DateTime? handledAt)
    {
        MessageId = messageId;
        Receiver = receiver;
        HandledAt = handledAt;
        ResponseState = MessageState.AcknowledgementReceived;
    }
        
    public ResponsePayload(Guid messageId, MessageReceiver receiver, DateTime? handledAt, TResponse response)
    {
        MessageId = messageId;
        Receiver = receiver;
        HandledAt = handledAt;
        Response = response;
        ResponseState = MessageState.ResponseReceived;
    }

    public ResponsePayload(Guid messageId, MessagingError error, MessageReceiver receiver)
    {
        MessageId = messageId;
        Error = error;
        Receiver = receiver;
        ResponseState = MessageState.Failed;
    }

    [JsonConstructor]
    private ResponsePayload()
    {
    }

    public ResponsePayload ToResponsePayload()
    {
        if (HasResponse)
        {
            string json_response = Response.ToJsonIgnoreCase();
            return new ResponsePayload(MessageId, Receiver, HandledAt, json_response);
        }
        else if (HasError)
        {
            return new ResponsePayload(MessageId, Error!, Receiver);
        }
        else
        {
            return new ResponsePayload(MessageId, Receiver, HandledAt);
        }
    }

    public static ResponsePayload<TResponse> FromResponsePayload(ResponsePayload responsePayload)
    {
        if (responsePayload.HasResponse)
        {
            TResponse response = responsePayload.ResponseJson!.ConvertToIgnoreCase<TResponse>();
            return new(responsePayload.MessageId, responsePayload.Receiver,responsePayload.HandledAt, response);
        }
        else if (responsePayload.HasError)
        {
            return new(responsePayload.MessageId, responsePayload.Error!, responsePayload.Receiver);
        }
        else
        {
            return new(responsePayload.MessageId, responsePayload.Receiver, responsePayload.HandledAt);
        }
    }
}