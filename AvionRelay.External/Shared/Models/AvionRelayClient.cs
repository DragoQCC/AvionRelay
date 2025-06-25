namespace AvionRelay.External;

/// <summary>
/// Represents the client side info for the Avion Relay connection
/// </summary>
public class AvionRelayClient
{
    /// <summary>
    /// Unique Identifier for this client, provided by the AvionRelay Server
    /// </summary>
    public Guid? ClientID { get; internal set; } = null;
    
    /// <summary>
    /// Friendly name of this client, can be used by other clients to send messages to this client <br/>
    /// Can be arbitrary but should be unique
    /// </summary>
    public required string ClientName { get; set; }
    
    /// <summary>
    /// The address of this client
    /// </summary>
    public Uri HostAddress { get; set; }
    
    /// <summary>
    /// The date time that this client connected, set after the client ID is set
    /// </summary>
    public DateTime ConnectedAt { get; internal set; }
    
    /// <summary>
    /// An arbitrary collection of key values to pass extra information as desired
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Should be the name of the message type that this client can process <br/>
    /// Recommended to use the nameof(type) ex. nameof(GetStatusCommand)
    /// </summary>
    public required List<string> SupportedMessages { get; set; }


    public AvionRelayClient(Guid ClientId)
    {
        HostAddress = Helper.GetPreferredIPAddress();
        ClientID = ClientId;
        ConnectedAt = DateTime.UtcNow;
        
    }
}