using AvionRelay.Core;

namespace AvionRelay.External;

public class AvionRelayExternalOptions : AvionRelayOptions
{
    /// <summary>
    /// Which transports are enabled for this hub instance
    /// </summary>
    public List<TransportTypes> EnabledTransports { get; set; } = new()
    {
        TransportTypes.SignalR
    };
   
    /// <summary>
    /// Transport-specific configurations
    /// </summary>
    public TransportOptions Transports { get; set; } = new();
}

public class TransportOptions
{
    public SignalROptions SignalR { get; set; } = new();
    public GrpcOptions Grpc { get; set; } = new();
    public RabbitMqOptions RabbitMq { get; set; } = new();
}

public class SignalROptions
{
    /// <summary>
    /// Hub endpoint path
    /// </summary>
    public string HubPath { get; set; }// = "/avionrelay";
    
    /// <summary>
    /// Maximum message size in bytes
    /// </summary>
    public int MaxMessageSize { get; set; }// = 10 * 1024 * 1024; // 10MB
    
    /// <summary>
    /// Client timeout in seconds
    /// </summary>
    public int ClientTimeoutSeconds { get; set; }// = 60;
    
    /// <summary>
    /// Keep alive interval in seconds
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; }// = 30;
    
    /// <summary>
    /// Enable detailed errors in responses
    /// </summary>
    public bool EnableDetailedErrors { get; set; }// = false;
    
    /// <summary>
    /// Maximum number of concurrent connections (0 = unlimited)
    /// </summary>
    public int MaxConcurrentConnections { get; set; }// = 0;
    
    /// <summary>
    /// Enable message tracing for debugging
    /// </summary>
    public bool EnableMessageTracing { get; set; }// = false;
}

public class GrpcOptions
{
    /// <summary>
    /// Listen address for gRPC service
    /// </summary>
    public string ListenAddress { get; set; } = "0.0.0.0:5002";
    
    /// <summary>
    /// Maximum message size in bytes
    /// </summary>
    public int MaxMessageSize { get; set; } = 4 * 1024 * 1024; // 4MB
    
    /// <summary>
    /// Enable TLS/SSL
    /// </summary>
    public bool EnableTls { get; set; } = true;
    
    /// <summary>
    /// Certificate path for TLS
    /// </summary>
    public string? CertificatePath { get; set; }
    
    /// <summary>
    /// Certificate password
    /// </summary>
    public string? CertificatePassword { get; set; }
    
    /// <summary>
    /// Enable gRPC reflection for debugging
    /// </summary>
    public bool EnableReflection { get; set; } = false;
    
    /// <summary>
    /// Enables detailed error messages from the Grpc services
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
}

public class RabbitMqOptions
{
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";
    public string ExchangeName { get; set; } = "avionrelay";
    public bool DurableQueues { get; set; } = true;
    public int PrefetchCount { get; set; } = 10;
}