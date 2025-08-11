using System.Collections.Concurrent;
using AvionRelay.Core.Dispatchers;
using HelpfulTypesAndExtensions;
using IntercomEventing.Features.Events;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.Services;

/// <summary>
/// Tracks pending responses for messages that expect responses
/// </summary>
public class ResponseTracker
{
    private readonly ConcurrentDictionary<Guid, PendingResponse> _pendingResponses = new();
    private readonly ILogger<ResponseTracker> _logger;
    private readonly AvionRelayExternalOptions _avionConfiguration;
    private readonly Timer _cleanupTimer;

    public MessageErrorSetEvent MessageErrorSetEvent = new();
    public MessageResponseSetEvent MessageResponseSetEvent = new();
    
    public ResponseTracker(AvionRelayExternalOptions avionConfiguration, ILogger<ResponseTracker> logger)
    {
        _avionConfiguration  = avionConfiguration;
        _logger = logger;
        
        // Cleanup expired pending responses every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredResponses, null, TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(300));
    }
    
    /// <summary>
    /// Registers a message that expects a response
    /// </summary>
    public void TrackPendingResponse(Guid messageId, string senderConnectionId, int desiredResponseCount)
    {
        var pendingResponse = new PendingResponse
        {
            MessageId = messageId,
            SenderConnectionId = senderConnectionId,
            ExpectedResponseCount = desiredResponseCount,
            CreatedAt = DateTime.UtcNow
        };
        
        _pendingResponses[messageId] = pendingResponse;
        _logger.LogDebug("Tracking pending response for message {MessageId} from {Sender}", messageId, senderConnectionId);
    }

    public void AddHandlerForTrackedMessage(Guid messageId, MessageReceiver handler)
    {
        if(_pendingResponses.TryGetValue(messageId, out var pendingResponse))
        {
            pendingResponse.ExcpectedResponders.TryAdd(handler.ReceiverId, new(handler));
        }
    }
    
    /// <summary>
    /// Records a response received for a message
    /// </summary>
    public void RecordResponse(Guid messageID, ResponsePayload response)
    {
        if (!_pendingResponses.TryGetValue(messageID, out var pendingResponse))
        {
            _logger.LogWarning("Received response for unknown message {MessageId}", messageID.ToString());
            return;
        }
        pendingResponse.Responses.Add(response);

        ExpectedResponder? responder = null;
        if (pendingResponse.ExcpectedResponders.TryGetValue(response.Receiver.ReceiverId, out responder) is false)
        {
            pendingResponse.ExcpectedResponders.TryGetValue(response.Receiver.Name, out responder);
        }
        if (responder is not null)
        {
            responder.SetResponse(response);
        }
        
        _logger.LogDebug("Recorded response {Number}/{Expected} for message {MessageId}", pendingResponse.Responses.Count, pendingResponse.ExpectedResponseCount, messageID);
    }

    public void MessageCleanupReady(Guid messageId)
    {
        if (_pendingResponses.TryGetValue(messageId, out var pendingResponse))
        {
            if (pendingResponse.Responses.Count >= pendingResponse.ExpectedResponseCount)
            {
                CompleteTracking(messageId);
            }
        }
    }

    public bool GotAllResponsesForMessage(Guid messageId)
    {
        if (_pendingResponses.TryGetValue(messageId, out var pendingResponse))
        {
            if (pendingResponse.Responses.Count >= pendingResponse.ExpectedResponseCount)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Gets the sender connection ID for a message
    /// </summary>
    public string? GetSenderConnectionId(Guid messageId)
    {
        return _pendingResponses.TryGetValue(messageId, out var pendingResponse) 
            ? pendingResponse.SenderConnectionId 
            : null;
    }
    
    /// <summary>
    /// Completes tracking for a message after responses have been sent
    /// </summary>
    public void CompleteTracking(Guid messageId)
    {
        _pendingResponses.TryRemove(messageId, out _);
    }

    
    public async Task SetMessagingErrorFor(TransportPackage transportPackage, MessageReceiver receiver, MessagingError error)
    {
        if (_pendingResponses.TryGetValue(transportPackage.MessageId, out var pendingResponse))
        {
            string receiverIdOrName = receiver.ReceiverId.IsEmpty() ? receiver.Name : receiver.ReceiverId;
            _logger.LogDebug("Setting error on receiver {ReceiverId} for message id: {MessageId}", receiverIdOrName, transportPackage.MessageId);
            ExpectedResponder? responder;
            if(pendingResponse.ExcpectedResponders.TryGetValue(receiverIdOrName, out responder) is false)
            {
                _logger.LogWarning("Attempted to set error on receiver {ReceiverId}, but did not find receiver is tracked", receiverIdOrName);
                responder = new ExpectedResponder(receiver);
                pendingResponse.ExcpectedResponders.Add(receiverIdOrName, responder);
            }
            responder.SetMessagingError(transportPackage.MessageId, error);
            await MessageErrorSetEvent.AlertForMessagingErrorSet(responder,transportPackage);
        }
        else
        {
            _logger.LogWarning("Attempted to set error on message ID {MessageId}, but did not find message in tracker", transportPackage.MessageId);
        }
    }
    
    private void CleanupExpiredResponses(object? state)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-5); // Clean up responses older than 5 minutes
        var expiredKeys = _pendingResponses
            .Where(kvp => kvp.Value.CreatedAt < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var key in expiredKeys)
        {
            _pendingResponses.TryRemove(key, out var pendingResponse);
        }
        
        if (expiredKeys.Any())
        {
            _logger.LogDebug("Cleaned up {Count} expired response trackers", expiredKeys.Count);
        }
    }

    public async IAsyncEnumerable<ExpectedResponder> TryGetFailedResponders(Guid messageID)
    {
        if(_pendingResponses.TryGetValue(messageID, out PendingResponse? pendingResponse))
        {
            foreach(ExpectedResponder failedResponder in pendingResponse.ExcpectedResponders.Values.Where(x => x.RespState is not ResponseState.Received))
            {
                yield return failedResponder;
            }
        }
    }
    
    private class PendingResponse
    {
        public Guid MessageId { get; init; }
        public string SenderConnectionId { get; init; } = "";
        public Dictionary<string,ExpectedResponder> ExcpectedResponders { get; init; } = new(); 
        public int ExpectedResponseCount { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<ResponsePayload> Responses { get; } = new();
        //public TimeSpan Timeout { get; init; }
        //public TaskCompletionSource<List<ResponsePayload>> ResponseTcs { get; init; } = new();
        //public CancellationTokenSource? TimeoutCancellation { get; set; }
    }
}

public class ExpectedResponder
{
    //who are we waiting on a response for?
    public MessageReceiver Receiver { get; private set; }
	
    //What is the state of getting a response back?
    public ResponseState RespState {get; set;}

    //Failure Count
    public int FailureCount = 0;
    
    public ResponsePayload? Response { get; private set; }

    public bool HasResult => RespState == ResponseState.Received; 

    public ExpectedResponder(MessageReceiver receiver)
    {
        Receiver = receiver;
    }

    public void SetMessagingError(Guid messageId, MessagingError error)
    {
        Response = new(messageId,error, Receiver);
        RespState = ResponseState.Error;
    }

    public void SetResponse(ResponsePayload response)
    {
        Response = response;
    }
}

//Assumption is the default state will be it was sent and we are waiting for something else to happen now
public enum ResponseState
{
    Waiting = 0,
    Received = 1,
    Error = 2
}

public record MessageErrorSetEvent : GenericEvent<MessageErrorSetEvent>
{
    public async Task AlertForMessagingErrorSet(ExpectedResponder targetClient, TransportPackage transportPackage)
    {
        var eventCall = new MessageErrorSetEventCall(targetClient, transportPackage);
        await RaiseEvent(eventCall);
    }

    /// <inheritdoc />
    override protected MessageErrorSetEventCall CreateEventCall(params object[]? args) => null;
}

public record MessageErrorSetEventCall(ExpectedResponder targetClient, TransportPackage transportPackage) : EventCall<MessageErrorSetEvent>;


public record MessageResponseSetEvent : GenericEvent<MessageResponseSetEvent>
{
    public async Task AlertForMessageResponseReceived(ResponsePayload response)
    {
        var eventCall = new MessageResponseSetEventCall(response);
        await RaiseEvent(eventCall);
    }
    
    /// <inheritdoc />
    override protected EventCall<MessageResponseSetEvent> CreateEventCall(params object[]? args) => null;
}

public record MessageResponseSetEventCall(ResponsePayload Response) : EventCall<MessageResponseSetEvent>;