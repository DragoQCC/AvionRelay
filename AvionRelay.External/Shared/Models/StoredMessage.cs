using AvionRelay.Core.Messages;

namespace AvionRelay.External;

//TODO: Evaluate if this is the right way to store messages & if I want this vs the other classes I already have?
public class StoredMessage
{
    public Guid MessageId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public BaseMessageType BaseMessageType { get; set; }
    public DateTime Timestamp { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public TransportTypes TransportType { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public int PayloadSize { get; set; }
    public Guid? CorrelationId { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
}