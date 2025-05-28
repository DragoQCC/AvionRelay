using SQLite;

namespace AvionRelay.External.Hub.Services;

[Table("MessageTypeStatistic")]
public class MessageTypeStatistic
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [Indexed]
    public string MessageType { get; set; } = string.Empty;
    
    public long Count { get; set; }
    
    public long TotalBytes { get; set; }
    
    public DateTime LastReceived { get; set; }
    
    public DateTime FirstReceived { get; set; }
}