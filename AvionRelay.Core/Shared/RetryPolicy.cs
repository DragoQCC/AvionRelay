using System.Text.Json.Serialization;
using AvionRelay.Core.Messages;

namespace AvionRelay.Core;

public class RetryPolicy
{
    [JsonIgnore]
    public Dictionary<MessagePriority, List<TimeSpan>> PriorityRetries { get; set; } = new();
    public int MaxRetryCount { get; set; }
    
    public void CreateWithDefaultPolicy()
    {
        foreach(MessagePriority priority in Enum.GetValues<MessagePriority>())
        {
          _ = priority switch
            {
                MessagePriority.Low => PriorityRetries.TryAdd(priority,[TimeSpan.FromSeconds(5)]),
                MessagePriority.Normal => PriorityRetries.TryAdd(priority,[TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(30)]),
                MessagePriority.High => PriorityRetries.TryAdd(priority,[TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(30)]),
                MessagePriority.VeryHigh => PriorityRetries.TryAdd(priority,[TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(30),TimeSpan.FromSeconds(60)]),
                MessagePriority.Critical => PriorityRetries.TryAdd(priority,[TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(30),TimeSpan.FromSeconds(60),TimeSpan.FromSeconds(300)]),
                _ => false //i.e., if its not a real priority ignore it
            };
        }
    }

    public RetryPolicy(int maxTryCount = 0)
    {
        MaxRetryCount = maxTryCount;
        CreateWithDefaultPolicy();
    }

    [JsonConstructor]
    private RetryPolicy()
    {
        
    }

}