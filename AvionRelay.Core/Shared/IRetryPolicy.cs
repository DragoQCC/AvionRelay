using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

public abstract class RetryPolicy
{
    public int MaxRetries { get; set; } = 5;
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    public abstract bool ShouldRetry<T>(AvionRelayMessage avionRelayMessage, Exception? exception) where T : AvionRelayMessage;
    
}

public class DefaultRetryPolicy : RetryPolicy
{
    public override bool ShouldRetry<T>(AvionRelayMessage avionRelayMessage, Exception? exception)
    {
        return avionRelayMessage.Metadata.RetryCount < MaxRetries;
    }
}