using AvionRelay.Core;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.Services;

public interface IMessageScheduler
{
    public TimeSpan? ShouldRetryDelivery(MessagePriority priority, ref int failureCount);
}

public class MessageSchedulerService : IMessageScheduler
{
    private readonly ILogger<MessageSchedulerService> _logger;
    private readonly AvionRelayOptions _avionRelayOptions;
    private readonly IExternalMessageStorage _externalMessageStorage;
    

    public MessageSchedulerService(AvionRelayOptions avionRelayOptions, IExternalMessageStorage externalMessageStorage, ILogger<MessageSchedulerService> logger)
    {
        _avionRelayOptions = avionRelayOptions;
        _logger = logger;
        _externalMessageStorage = externalMessageStorage;
    }
    
    /// <summary>
    /// Determine if a message should be retransmitted or not
    /// </summary>
    /// <param name="priority"></param>
    /// <param name="failureCount"></param>
    /// <returns>A valid TimeSpan if any retry attempts for this message are allowed, otherwise null</returns>
    public TimeSpan? ShouldRetryDelivery(MessagePriority priority, ref int failureCount)
    {
        RetryPolicy policy = _avionRelayOptions.RetryPolicy;
        if (policy.PriorityRetries.TryGetValue(priority, out List<TimeSpan>? priorityLevelRetries) is false)
        {
            _logger.LogWarning("No time spans provided for {Priority} in retry policy", priority.ToString());
            return null;
        }
        if (failureCount < policy.MaxRetryCount)
        {
            TimeSpan waitingPeriod = new();
            //If the priority level does not have a specific time limit set for this failure count, just return the last time limit set by this priority
            if (priorityLevelRetries.Count <= failureCount)
            {
                waitingPeriod = priorityLevelRetries.Last();
            }
            else
            {
                waitingPeriod = priorityLevelRetries[failureCount];
            }
            failureCount++;
            _logger.LogDebug("Message on retry {attempt}/{allowed}", failureCount, policy.MaxRetryCount);
            _logger.LogDebug("Returning time span of {waitingTime}", waitingPeriod);
            return waitingPeriod;
        }
        _logger.LogDebug("Failure count exceeded max retry limit");
        return null;
    }

}
