namespace AvionRelay.External;

public record MessageHandlerRegistration
{
    public required string HandlerID { get; init; }
    public required List<string> MessageNames { get; set; }
}


/*/// <summary>
/// Represents a command type that a client can handle
/// </summary>
public record CommandTypeRegistration
{
    public required string TypeName { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public Dictionary<string, string>? Parameters { get; init; } // Parameter name -> description
}*/