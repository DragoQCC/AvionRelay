using System.Collections.Concurrent;

namespace AvionRelay.External;

//Tracks message statistics
public class MessageStatistics
{
    private long _totalMessagesReceived;
    private long _totalMessagesSent;
    private long _totalBytesReceived;
    private long _totalBytesSent;
    private readonly ConcurrentDictionary<string, MessageTypeStats> _messageTypeStats = new();

    public class MessageTypeStats
    {
        public long Count { get; set; }
        public long TotalBytes { get; set; }
        public DateTime LastReceived { get; set; }
        public double AverageSize => Count > 0 ? TotalBytes / (double)Count : 0;
    }

    public void RecordMessageReceived(string messageType, int sizeInBytes)
    {
        Interlocked.Increment(ref _totalMessagesReceived);
        Interlocked.Add(ref _totalBytesReceived, sizeInBytes);

        _messageTypeStats.AddOrUpdate(messageType,
          new MessageTypeStats
          {
              Count = 1,
              TotalBytes = sizeInBytes,
              LastReceived = DateTime.UtcNow
          },
          (_, stats) =>
          {
              stats.Count++;
              stats.TotalBytes += sizeInBytes;
              stats.LastReceived = DateTime.UtcNow;
              return stats;
          });
    }

    public SignalRMessageStatisticsSnapshot GetSnapshot() => new()
    {
        TotalMessagesReceived = _totalMessagesReceived,
        TotalMessagesSent = _totalMessagesSent,
        TotalBytesReceived = _totalBytesReceived,
        TotalBytesSent = _totalBytesSent,
        MessageTypeStats = _messageTypeStats.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
        )
    };

    public record SignalRMessageStatisticsSnapshot
    {
        public long TotalMessagesReceived { get; init; }
        public long TotalMessagesSent { get; init; }
        public long TotalBytesReceived { get; init; }
        public long TotalBytesSent { get; init; }
        public Dictionary<string, MessageTypeStats> MessageTypeStats { get; init; } = new();
    }
}