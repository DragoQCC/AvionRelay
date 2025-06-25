namespace AvionRelay.External.Transports.SignalR;


public class SignalRTransportOptions
{
    /// <summary>URL to the AvionRelay Hub (e.g., https://hub.example.com/avionrelay)</summary>
    public string HubUrl { get; set; } = "https://localhost:5001/avionrelay";
    
    /// <summary>Reconnection policy settings</summary>
    public ReconnectionPolicy Reconnection { get; set; } = new();
    
    
    public class ReconnectionPolicy
    {
        public int MaxAttempts { get; set; } = 5;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    }
}