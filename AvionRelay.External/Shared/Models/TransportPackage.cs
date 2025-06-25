using System.Text.Json;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External;

public record TransportPackage
{
    public Guid MessageId { get; init; }
    public string MessageTypeName { get; init; } = string.Empty;
    public BaseMessageType BaseMessageType { get; init; }
    public string MessageJson { get; init; } = string.Empty;
    public MessagePriority Priority { get; init; }
    public DateTime CreatedAt { get; init; }
    public string SenderId { get; init; } = string.Empty;
    /// <summary>
    /// For Commands and Inspections this will be populated, otherwise it will be empty
    /// </summary>
    public List<string> HandlerIdsOrNames { get; init; } = [ ];
    
   
}

/// <summary>
/// Updated TransportPackage.ToPackage() method using the resolver
/// </summary>
public static class TransportPackageExtensions
{
    private static ITypeResolver? _typeResolver;
    private static ILogger? _logger;

    /// <summary>
    /// Initialize the static type resolver (call this during startup)
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _typeResolver = serviceProvider.GetRequiredService<ITypeResolver>();
        _logger = serviceProvider.GetService<ILogger<TransportPackage>>();
    }

    /// <summary>
    /// Converts back to a Package on the receiving end (requires knowledge of concrete types)
    /// </summary>
    public static Package? ToPackage(this TransportPackage transportPackage)
    {
        if (_typeResolver == null)
        {
            throw new InvalidOperationException("Type resolver not initialized. Call TransportPackageExtensions.Initialize during startup.");
        }

        // Try to get the exact type first
        var messageType = _typeResolver.GetType(transportPackage.MessageTypeName);
            
        if (messageType == null)
        {
            _logger?.LogWarning("Type {TypeName} not found, attempting fallback resolution",  transportPackage.MessageTypeName);
            return null;
        }

        _logger?.LogDebug("Using resolved type {FullName} for deserialization", messageType.FullName);
        
        JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        
        var message = JsonSerializer.Deserialize(transportPackage.MessageJson, messageType,jsonOptions) as AvionRelayMessage
            ?? throw new InvalidOperationException($"Failed to deserialize message of type: {messageType.FullName}");

        return Package.Create(message);
    }
    
    /// <summary>
    /// Creates a transport package from a core package
    /// </summary>
    public static TransportPackage FromPackage(Package package, string senderId = "")
    {
        if (_typeResolver == null)
        {
            throw new InvalidOperationException("Type resolver not initialized. Call TransportPackageExtensions.Initialize during startup.");
        }

        // Try to get the exact type first
        var messageType = _typeResolver.GetType(package.Message.Metadata.MessageTypeName);
        
        return new TransportPackage
        {
            MessageId = package.WrapperID,
            MessageTypeName = package.Message.Metadata.MessageTypeName,
            BaseMessageType = package.Message.Metadata.BaseMessageType,
            MessageJson = JsonSerializer.Serialize(package.Message, messageType),
            Priority = package.Message.Metadata.Priority,
            CreatedAt = package.Message.Metadata.CreatedAt.DateTime,
            SenderId = senderId
        };
    }
}