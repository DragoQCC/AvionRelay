using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

public interface IRetryPolicy
{
    public int MaxRetries { get; }
    public TimeSpan RetryInterval { get; }
    
    public bool ShouldRetry(AvionRelayMessageBase message, Exception? exception);
    
    public Task RetryAsync(AvionRelayMessageBase message);
    
}