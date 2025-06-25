namespace AvionRelay.External;

/// <summary>
/// Data for the client we expect to receive from the client itself
/// </summary>
/*public record ClientRegistration
{
    public required string ClientId { get; init; }
    public required string ClientName { get; init; }
    public required TransportTypes TransportType { get; init; }
    public required Uri HostAddress { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Should be the name of the message type that this client can process <br/>
    /// Recommended to use the nameof(type) ex. nameof(GetStatusCommand)
    /// </summary>
    public List<string> SupportedMessages { get; init; } = new();
}*/

public class ClientRegistrationRequest
{
    public required string ClientName { get; init; }
    
    public string ClientVersion { get; init; }
    
    public required TransportTypes TransportType { get; init; }
    
    public required Uri HostAddress { get; init; }
    
    /// <summary>
    /// 
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// The Message types names that this client can process <br/>
    /// Should be the "short name" of the type <br/>
    /// Can be obtained with nameof(TypeName) 
    /// </summary>
    public List<string> SupportedMessages { get; init; } = new();
}

public class ClientRegistrationResponse
{
    public required Guid ClientId { get; set; }
    public bool Success { get; set; }
    public string? FailureMessage { get; set; } = null;
    public required string ServerVersion { get; set; } = "";
    List<string> ServerCapabilities = new();
}