using System.Collections.Concurrent;
using AvionRelay.Core.Messages;

namespace AvionRelay.External.Hub.Services;

/// <summary>
/// Tracks pending responses for messages that expect responses
/// </summary>
public class ResponseTracker
{
    private readonly ConcurrentDictionary<Guid, PendingResponse> _pendingResponses = new();
    private readonly ILogger<ResponseTracker> _logger;
    private readonly Timer _cleanupTimer;
    
    public ResponseTracker(ILogger<ResponseTracker> logger)
    {
        _logger = logger;
        
        // Cleanup expired pending responses every 30 seconds
        _cleanupTimer = new Timer(CleanupExpiredResponses, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    /// <summary>
    /// Registers a message that expects a response
    /// </summary>
    public void TrackPendingResponse(Guid messageId, string senderConnectionId, int expectedResponseCount = 1, TimeSpan? timeout = null)
    {
        var pendingResponse = new PendingResponse
        {
            MessageId = messageId,
            SenderConnectionId = senderConnectionId,
            ExpectedResponseCount = expectedResponseCount,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            CreatedAt = DateTime.UtcNow,
            ResponseTcs = new TaskCompletionSource<List<MessageResponse<object>>>()
        };
        
        _pendingResponses[messageId] = pendingResponse;
        
        // Set up timeout
        var cts = new CancellationTokenSource(pendingResponse.Timeout);
        cts.Token.Register(() =>
        {
            if (_pendingResponses.TryRemove(messageId, out var response))
            {
                response.ResponseTcs.TrySetException(
                    new TimeoutException($"Response timeout for message {messageId}"));
                _logger.LogWarning("Response timeout for message {MessageId} after {Timeout}",
                    messageId, pendingResponse.Timeout);
            }
        });
        
        pendingResponse.TimeoutCancellation = cts;
        
        _logger.LogDebug("Tracking response for message {MessageId} from {Sender}",
            messageId, senderConnectionId);
    }
    
    /// <summary>
    /// Records a response received for a message
    /// </summary>
    public bool RecordResponse<TResponse>(Guid messageId, string responderId, TResponse response)
    {
        if (!_pendingResponses.TryGetValue(messageId, out var pendingResponse))
        {
            _logger.LogWarning("Received response for unknown message {MessageId}", messageId);
            return false;
        }
        
        var messageResponse = new MessageResponse<object>
        {
            MessageId = messageId,
            Acknowledger = new Core.Dispatchers.MessageReceiver(responderId, responderId),
            Response = response!
        };
        
        pendingResponse.Responses.Add(messageResponse);
        
        _logger.LogDebug("Recorded response {Number}/{Expected} for message {MessageId}",
            pendingResponse.Responses.Count, pendingResponse.ExpectedResponseCount, messageId);
        
        // Check if we've received all expected responses
        if (pendingResponse.Responses.Count >= pendingResponse.ExpectedResponseCount)
        {
            if (_pendingResponses.TryRemove(messageId, out _))
            {
                pendingResponse.TimeoutCancellation?.Cancel();
                pendingResponse.ResponseTcs.TrySetResult(pendingResponse.Responses);
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Waits for all responses to a message
    /// </summary>
    public async Task<List<MessageResponse<object>>> WaitForResponsesAsync(Guid messageId)
    {
        if (!_pendingResponses.TryGetValue(messageId, out var pendingResponse))
        {
            throw new InvalidOperationException($"No pending response tracked for message {messageId}");
        }
        
        var responses = await pendingResponse.ResponseTcs.Task;
        
       return responses;
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
    /// Cancels tracking for a message
    /// </summary>
    public void CancelTracking(Guid messageId)
    {
        if (_pendingResponses.TryRemove(messageId, out var pendingResponse))
        {
            pendingResponse.TimeoutCancellation?.Cancel();
            pendingResponse.ResponseTcs.TrySetCanceled();
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
            if (_pendingResponses.TryRemove(key, out var pendingResponse))
            {
                pendingResponse.TimeoutCancellation?.Cancel();
                pendingResponse.ResponseTcs.TrySetException(
                    new TimeoutException("Response tracking expired"));
            }
        }
        
        if (expiredKeys.Any())
        {
            _logger.LogDebug("Cleaned up {Count} expired response trackers", expiredKeys.Count);
        }
    }
    
    private class PendingResponse
    {
        public Guid MessageId { get; init; }
        public string SenderConnectionId { get; init; } = "";
        public int ExpectedResponseCount { get; init; }
        public TimeSpan Timeout { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<MessageResponse<object>> Responses { get; } = new();
        public TaskCompletionSource<List<MessageResponse<object>>> ResponseTcs { get; init; } = new();
        public CancellationTokenSource? TimeoutCancellation { get; set; }
    }
}