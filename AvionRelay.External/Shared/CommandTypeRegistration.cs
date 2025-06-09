namespace AvionRelay.External;

public record MessageHandlerRegistration
{
    public required string HandlerID { get; init; }
    public required List<string> MessageNames { get; set; }
}