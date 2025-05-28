namespace AvionRelay.Core.Dispatchers;

public record MessageReceiver
{
    public string ReceiverId { get; init; }
    public string? Name { get; init; }

    public MessageReceiver(string receiverId, string? name)
    {
        ReceiverId = receiverId;
        Name = name;
    }
}

public record ExternalMessageReceiver : MessageReceiver
{
    public ExternalInformation ExternalInformation { get; init; }

    public ExternalMessageReceiver(string receiverId, string? name, ExternalInformation externalInformation) : base(receiverId, name)
    {
        ExternalInformation = externalInformation;
    }
}

public record ExternalInformation
{
    public Uri Endpoint { get; set; }
    public string TransportType { get; set; }
    public string? ApplicationName { get; set; }
    
    public ExternalInformation(Uri endpoint, string transportType, string? applicationName = null)
    {
        Endpoint = endpoint;
        TransportType = transportType;
        ApplicationName = applicationName;
    }
}