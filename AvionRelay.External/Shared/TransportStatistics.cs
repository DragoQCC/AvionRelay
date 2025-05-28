namespace AvionRelay.External;

public class TransportStatistics
{
    public TransportTypes TransportType { get; init; } = TransportTypes.Unknown;
    public int ActiveConnections { get; init; }
    public long TotalMessagesReceived { get; init; }
    public long TotalMessagesSent { get; init; }
    public long TotalBytesReceived { get; init; }
    public long TotalBytesSent { get; init; }
    public Dictionary<string, long> MessageTypeCounts { get; init; } = new();
    public DateTime StartTime { get; init; }
}