using System.Reflection;
using System.Text.Json;
using AvionRelay.Core.Messages;

namespace AvionRelay.External;

/*public record RoutedMessage
{
    public Guid MessageID { get; set; }
    public required string SenderId { get; set; }
    public required string MessageName { get; set; }
    /// <summary>
    /// Is a <see cref="Package"/> type
    /// </summary>
    public required TransportPackage Package { get; set; }
}*/

public record TransportPackage
{
    public Guid MessageId { get; init; }
    public string MessageTypeShortName { get; init; } = string.Empty;
    public string MessageTypeFullName { get; init; } = string.Empty;
    public string BaseMessageType { get; init; } = string.Empty;
    public string MessageJson { get; init; } = string.Empty;
    public MessagePriority Priority { get; init; }
    public DateTime CreatedAt { get; init; }
    public string SenderId { get; init; } = string.Empty;
    
    /// <summary>
    /// Creates a transport package from a core package
    /// </summary>
    public static TransportPackage FromPackage(Package package, string senderId = "")
    {
        return new TransportPackage
        {
            MessageId = package.WrapperID,
            MessageTypeShortName = package.MessageType,
            MessageTypeFullName = package.Message.GetType().AssemblyQualifiedName,
            BaseMessageType = package.Message.Metadata.BaseMessageType.ToString(),
            MessageJson = JsonSerializer.Serialize(package.Message),
            Priority = package.Message.Metadata.Priority,
            CreatedAt = package.Message.Metadata.CreatedAt.DateTime,
            SenderId = senderId
        };
    }
    
    /// <summary>
    /// Converts back to a Package on the receiving end (requires knowledge of concrete types)
    /// </summary>
    public Package ToPackage()
    {
        // This would be called on the client side where concrete types are known
        var messageType = Type.GetType(MessageTypeFullName) 
            ?? throw new InvalidOperationException($"Unknown message type: {MessageTypeFullName}");
        
        Console.WriteLine($"Deserialized message type: {MessageTypeFullName}");
        
        var message = JsonSerializer.Deserialize(MessageJson, messageType) as AvionRelayMessage
            ?? throw new InvalidOperationException($"Failed to deserialize message of type: {MessageTypeFullName}");
        
        return Package.Create(message);
    }
}