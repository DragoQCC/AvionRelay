namespace AvionRelay.External.Transports.SignalR;


public class SignalRTransportOptions
{
    /// <summary>URL to the AvionRelay Hub (e.g., https://hub.example.com/avionrelay)</summary>
    public string HubUrl { get; set; } = "https://localhost:5001/avionrelay";
    
    /// <summary>Client identifier for this application instance</summary>
    public string ClientId { get; set; } = Guid.CreateVersion7().ToString();
    
    /// <summary>Friendly name for this client</summary>
    public string ClientName { get; set; } = Environment.MachineName;
    
    /// <summary>Optional group ID to join</summary>
    public string? GroupId { get; set; }
    
    /// <summary>Reconnection policy settings</summary>
    public ReconnectionPolicy Reconnection { get; set; } = new();
    
    
    public class ReconnectionPolicy
    {
        public int MaxAttempts { get; set; } = 5;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    }
}