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
            ResponseTcs = new TaskCompletionSource<List<JsonResponse>>()
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
                _logger.LogWarning("Response timeout for message {MessageId} after {Timeout}", messageId, pendingResponse.Timeout);
            }
        });
        
        pendingResponse.TimeoutCancellation = cts;
        
        _logger.LogDebug("Tracking response for message {MessageId} from {Sender}", messageId, senderConnectionId);
    }
    
    /// <summary>
    /// Records a response received for a message
    /// </summary>
    public bool RecordResponse(Guid messageId, string responderId, JsonResponse response)
    {
        if (!_pendingResponses.TryGetValue(messageId, out var pendingResponse))
        {
            _logger.LogWarning("Received response for unknown message {MessageId}", messageId);
            return false;
        }
        
       
        pendingResponse.Responses.Add(response);
        
        _logger.LogDebug("Recorded response {Number}/{Expected} for message {MessageId}",
            pendingResponse.Responses.Count, pendingResponse.ExpectedResponseCount, messageId);
        
        // Check if we've received all expected responses
        if (pendingResponse.Responses.Count >= pendingResponse.ExpectedResponseCount)
        {
            //todo: See if removing this allows tasking to flow correctly vs removing the tracked result early
            //pendingResponse.TimeoutCancellation?.Cancel();
            pendingResponse.ResponseTcs.TrySetResult(pendingResponse.Responses);
            _logger.LogDebug("All responses received for message {MessageId}", messageId);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Waits for all responses to a message
    /// </summary>
    public async Task<List<JsonResponse>> WaitForResponsesAsync(Guid messageId)
    {
        if (!_pendingResponses.TryGetValue(messageId, out var pendingResponse))
        {
            _logger.LogDebug("Waiting for responses for the following messages: {PendingIDs} ", string.Join(",",_pendingResponses.Keys.Select(k => k.ToString())));
            throw new InvalidOperationException($"No pending response tracked for message {messageId}");
        }
        
        try
        {
            var responses = await pendingResponse.ResponseTcs.Task;
            if (pendingResponse.Responses.Count >= pendingResponse.ExpectedResponseCount)
            {
                CompleteTracking(messageId);
            }
            return responses;
        }
        catch (TimeoutException)
        {
            // Return partial responses on timeout
            _logger.LogWarning("Returning {Count} partial responses for message {MessageId} after timeout",  pendingResponse.Responses.Count, messageId);
            return pendingResponse.Responses;
        }
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
        if (_pendingResponses.TryRemove(messageId, out var pendingResponse))
        {
            pendingResponse.TimeoutCancellation?.Dispose();
            _logger.LogDebug("Completed tracking for message {MessageId}", messageId);
        }
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
        public List<JsonResponse> Responses { get; } = new();
        public TaskCompletionSource<List<JsonResponse>> ResponseTcs { get; init; } = new();
        public CancellationTokenSource? TimeoutCancellation { get; set; }
    }
}