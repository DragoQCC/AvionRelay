using SQLite;

namespace AvionRelay.External.Server.Services;

[Table("ConnectionRecord")]
public class ConnectionRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string ConnectionId { get; set; } = string.Empty;
    
    public string ClientName { get; set; } = string.Empty;
    
    public string? GroupId { get; set; }
    
    [Indexed]
    public DateTime ConnectedAt { get; set; }
    
    public DateTime? DisconnectedAt { get; set; }
    
    public string MetadataJson { get; set; } = string.Empty;
}