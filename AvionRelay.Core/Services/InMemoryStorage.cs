using System.Collections.Concurrent;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Core.Services;

public class InMemoryStorage : IMessageStorage
{
    private readonly PriorityQueue<Package, MessagePriority> _queue = new(Comparer<MessagePriority>.Create((x, y) => y.CompareTo(x)));
    
    private readonly ConcurrentDictionary<Guid, Package> _packages = new();
    
    private readonly ILogger<InMemoryStorage> _logger;
    private readonly MessagingManager _messagingManager;
    
    public InMemoryStorage(ILogger<InMemoryStorage> logger, MessagingManager messagingManager)
    {
        _logger = logger;
        _messagingManager = messagingManager;
    }
   
    /// <inheritdoc />
    public void StorePackage(Package package, bool inQueue = false)
    {
        if (inQueue)
        {
            _logger.LogDebug("Enqueued message with priority {Priority} to queue", package.Message.Metadata.Priority);
            _queue.Enqueue(package, package.Message.Metadata.Priority);
        }
        _packages.TryAdd(package.Message.Metadata.MessageId, package);
    }

    /// <inheritdoc />
    public Package? RetrievePackage(Guid messageId)
    {
        if (_packages.TryGetValue(messageId, out var package))
        {
            if(_messagingManager.IsMessageComplete(package.Message))
            {
                _logger.LogDebug("Message {MessageId} is in a finalized state, removing from storage", messageId);
                _packages.TryRemove(messageId, out _);
            }
            return package;
        }
        return null;
    }
    
    /// <summary>
    /// For an in-memory queue, we don't need to worry about message IDs
    /// The "next" package is whatever is at the front of the queue
    /// </summary>
    /// <returns></returns>
    public Package? RetrieveNextPackage()
    {
        if (_queue.TryDequeue(out var package, out var priority))
        {
            _logger.LogDebug("Dequeued message with priority {Priority} from queue", priority);
            return package;
        }
        return null;
    }
}