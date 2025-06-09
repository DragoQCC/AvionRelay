namespace AvionRelay.External.Server.SignalR;

public class SignalRHubOptions
{
    public bool EnableDetailedErrors { get; set; } = true;
    public int MaxMessageSize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int ClientTimeoutSeconds { get; set; } = 60;
    public int KeepAliveIntervalSeconds { get; set; } = 30;
}