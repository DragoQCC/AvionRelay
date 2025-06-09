namespace AvionRelay.External.Server.Models;

public class AggregatedStatistics
{
    public Dictionary<TransportTypes, TransportStatistics> TransportStatistics { get; } = new();
    public int TotalActiveConnections { get; set; }
    public long TotalMessagesReceived { get; set; }
    public long TotalMessagesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    public long TotalBytesSent { get; set; }
    public Dictionary<string, long> MessageTypeCountsTotal { get; } = new();
}