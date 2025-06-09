using System.Text.Json;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External;

/// <summary>
/// Service that handles message deserialization using type resolution
/// </summary>
public class MessageDeserializationService
{
    private readonly ITypeResolver _typeResolver;
    private readonly ILogger<MessageDeserializationService> _logger;

    public MessageDeserializationService(
        ITypeResolver typeResolver,
        ILogger<MessageDeserializationService> logger)
    {
        _typeResolver = typeResolver;
        _logger = logger;
    }

    /// <summary>
    /// Deserializes a message from JSON using the type resolver
    /// </summary>
    public AvionRelayMessage? DeserializeMessage(string messageTypeName, string messageJson)
    {
        try
        {
            // Resolve the type
            var messageType = _typeResolver.GetType(messageTypeName);
            if (messageType == null)
            {
                _logger.LogError("Could not resolve message type: {TypeName}", messageTypeName);
                return null;
            }

            _logger.LogDebug("Resolved {TypeName} to {FullName}", 
                messageTypeName, messageType.FullName);

            // Deserialize the JSON
            var message = JsonSerializer.Deserialize(messageJson, messageType) as AvionRelayMessage;
            
            if (message == null)
            {
                _logger.LogError("Failed to deserialize message of type {TypeName}", messageTypeName);
                return null;
            }

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing message {TypeName}", messageTypeName);
            return null;
        }
    }

    /// <summary>
    /// Creates a Package from a transport package using type resolution
    /// </summary>
    public Package? CreatePackageFromTransport(TransportPackage transportPackage)
    {
        var message = DeserializeMessage(
            transportPackage.MessageTypeShortName, 
            transportPackage.MessageJson);

        if (message == null)
            return null;

        // Create the package
        var package = new Package
        {
            Message = message,
            MessageType = transportPackage.MessageTypeShortName,
            WrapperID = transportPackage.MessageId
        };

        return package;
    }
}