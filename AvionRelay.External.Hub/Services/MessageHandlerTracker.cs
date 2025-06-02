using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AvionRelay.External.Hub.Services;

public class MessageHandlerTracker
{
    /// <summary>
    /// Key: The name of the message type,
    /// Value: A list of client IDs that have registered to handle this message type
    /// </summary>
    ConcurrentDictionary<string, HashSet<string>> MessageHandlers { get; init; } = new();
    
    private readonly ILogger<MessageHandlerTracker> _logger;

    public MessageHandlerTracker(ILogger<MessageHandlerTracker> logger)
    {
        _logger = logger;
    }

    public Task AddMessageHandler(MessageHandlerRegistration clientRegistration)
    {
        foreach (string messageName in clientRegistration.MessageNames)
        {
            _logger.LogInformation("Adding message handler for message {MessageName}", messageName);
            MessageHandlers.AddOrUpdate(messageName, (key) => [clientRegistration.HandlerID], (key, handlers) =>
            {
                handlers.Add(clientRegistration.HandlerID);
                return handlers;
            });
        }
        return Task.CompletedTask;
    }

    public List<string> GetMessageHandlers(string messageName)
    {
        MessageHandlers.TryGetValue(messageName, out var handlers);
        return handlers?.ToList() ?? [];
    }
    
}