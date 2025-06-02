using System.Collections.Concurrent;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;

namespace AvionRelay.Core.Services;

public static class MessageHandlerRegister
{
    private static ConcurrentDictionary<Type,HashSet<IAvionRelayMessageSubscription>> _subscriptions = new();
    
    
    public static async Task RegisterHandler<TMessage>(MessageReceiver receiver, Func<TMessage, Task> handler, Func<Task>? onSubscribe = null, Func<Task>? onUnsubscribe = null) where TMessage : AvionRelayMessage
    {
        var subscription = new MessageSubscription<TMessage>(receiver,handler,onSubscribe,onUnsubscribe);
        
        var isSingleReceiver = typeof(TMessage).IsAssignableTo(typeof(ISingleReceiver));
        var messageType = typeof(TMessage);
        
        if (isSingleReceiver && _subscriptions.ContainsKey(messageType))
        {
            throw new Exception($"Message type {messageType} only allows for one receiver and is already subscribed.");
        }
        else
        {
            _subscriptions.AddOrUpdate(messageType, [ subscription ], (key, value) =>
            {
                value.Add(subscription);
                return value;
            });
        }
        Console.WriteLine($"Registered handler for {messageType}");
        await subscription.OnSubscribe();

    }

    public static async Task<bool> RemoveHandler(MessageReceiver receiver)
    {
        foreach (HashSet<IAvionRelayMessageSubscription> avionRelayMessageSubscriptions in  _subscriptions.Values)
        {
            //if the message subscriptions contains a MessageReceiver that matches then remove it
            if (avionRelayMessageSubscriptions.Any(s => s.MessageReceiver.ReceiverId == receiver.ReceiverId))
            {
                var s = avionRelayMessageSubscriptions.First(s => s.MessageReceiver.ReceiverId == receiver.ReceiverId);
                avionRelayMessageSubscriptions.Remove(s);
                await s.OnUnsubscribe();
                return true;
            }
        }
        return false;
    }
    
    public static bool TryGetHandlers<TMessage>(out HashSet<IAvionRelayMessageSubscription> handlers) where TMessage : AvionRelayMessage
    {
        var messageType = typeof(TMessage);
        return _subscriptions.TryGetValue(messageType, out handlers);
    }
    
    public static bool TryGetHandlers(Type messageType, out HashSet<IAvionRelayMessageSubscription>? handlers) => _subscriptions.TryGetValue(messageType, out handlers);
    
    public static async Task<List<MessageReceiver>> GetUniqueMessageHandlers(Type messageType)
    {
        if (TryGetHandlers(messageType, out var handlers))
        {
            return handlers.Select(s => s.MessageReceiver).ToList();
        }
        return new List<MessageReceiver>();
    }
    
    public static int GetReceiverCount(Type messageType)
    {
        if (TryGetHandlers(messageType, out var handlers))
        {
            return handlers.Select(s => s.MessageReceiver).Distinct().Count();
        }
        return 0;
    }
    
    public static int GetHandlerCount(Type messageType)
    {
        if (TryGetHandlers(messageType, out var handlers))
        {
            return handlers.Count;
        }
        return 0;
    }

    public static async Task ProcessPackage(Package package)
    {
        Console.WriteLine("Processing package");
        try
        {
            //get the type for the message 
            Type messageType = package.Message.GetType();
            Console.WriteLine($"message type: {messageType}");
        
            if (TryGetHandlers(messageType, out var handlers))
            {
                Console.WriteLine($"found handler for {messageType}");
                foreach (var handler in handlers ?? [])
                {
                    try
                    {
                        //set the message state to processing
                        package.Message.Metadata.State = MessageState.Processing;
                        await handler.HandleAsync(package);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Received package but no handlers found for type {messageType}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static List<Type> GetMessageTypes()
    {
        var messageTypes = new List<Type>();
        foreach (var subscription in _subscriptions)
        {
            if (messageTypes.Contains(subscription.Key))
            {
                continue;
            }
            messageTypes.Add(subscription.Key);
        }
        return messageTypes;
    }
}

