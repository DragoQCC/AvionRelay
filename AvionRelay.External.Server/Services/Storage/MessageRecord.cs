using SQLite;

namespace AvionRelay.External.Server.Services;

[Table("MessageRecord")]
public class MessageRecord
{
    [PrimaryKey]
    public Guid MessageId { get; set; }
    
    [Indexed]
    public string MessageType { get; set; } = string.Empty;
    
    public string MessagePattern { get; set; } = string.Empty;
    
    [Indexed]
    public DateTime Timestamp { get; set; }
    
    public string SenderId { get; set; } = string.Empty;
    
    [Indexed]
    public Guid? CorrelationId { get; set; }
    
    public string PayloadJson { get; set; } = string.Empty;
    
    public int PayloadSize { get; set; }
    
    public bool IsProcessed { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
}