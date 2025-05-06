using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;
using AvionRelay.Internal;
using ConsoleExample.MessageComponents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleExample;

/// <summary>
/// Example demonstrating the internal message broker.
/// </summary>
public static class InternalMessageBrokerExample
{
    /// <summary>
    /// Runs the example.
    /// </summary>
    public static async Task RunExample()
    {
        Console.WriteLine("Starting Internal Message Broker Example");
        Console.WriteLine("=======================================\n");
        
        // Set up DI
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add the internal message dispatcher
        services.AddInternalMessageDispatcher();
        
        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        
        // Get the sender, receiver, and broker
        var sender = serviceProvider.GetRequiredService<AvionInternalSender>();
        var receiver = serviceProvider.GetRequiredService<AvionInternalReceiver>();
        var broker = serviceProvider.GetRequiredService<InternalMessageBroker>();
        
        // Subscribe to messages
        broker.Subscribe<ExampleCommand>(async envelope =>
        {
            Console.WriteLine($"Broker received message {envelope.Message.MessageId}");
            
            // Acknowledge the message
            envelope.Message.Acknowledge();
            
            // Change the state to Acknowledged
            envelope.Progress.ChangeStateTo(new Acknowledged());
            
            // Simulate some processing
            await Task.Delay(100);
            
            // Respond to the message
            broker.RespondToMessage<ExampleCommand, ExampleResponse>(
                envelope.Message.MessageId, 
                new ExampleResponse($"Response to {envelope.Message.MessageId}"));
            
            Console.WriteLine($"Broker sent response to message {envelope.Message.MessageId}");
        });
        
        // Start listening for messages
        receiver.StartListening();
        
        // Send a message and wait for a response
        await SendMessageAndWaitForResponse(sender);
        
        // Send messages with different priorities
        await SendMessagesWithDifferentPriorities(sender);
        
        // Wait for all messages to be processed
        await Task.Delay(1000);
        
        // Stop listening for messages
        await receiver.StopListening();
        
        Console.WriteLine("\nInternal Message Broker Example complete!");
    }
    
    private static async Task SendMessageAndWaitForResponse(AvionInternalSender sender)
    {
        Console.WriteLine("\nSending a message and waiting for a response...");
        
        // Create a message
        var message = new ExampleCommand();
        
        // Create a message context
        var context = new MessageContext { Priority = MessagePriority.Normal };
        
        // Create a message envelope
        var envelope = new MessageEnvelope<ExampleCommand>(
            message,
            context,
            sender,
            new AvionInternalReceiver(null, null))
        {
            Progress = new MessageProgress(new Created())
        };
        
        // Send the message and wait for a response
        var response = await sender.SendAndWaitForResponseAsync<ExampleCommand, ExampleResponse>(
            envelope, 
            TimeSpan.FromSeconds(5));
        
        if (response != null)
        {
            Console.WriteLine($"Received response: {response.Result}");
        }
        else
        {
            Console.WriteLine("No response received within the timeout period");
        }
    }
    
    private static async Task SendMessagesWithDifferentPriorities(AvionInternalSender sender)
    {
        Console.WriteLine("\nSending messages with different priorities...");
        
        // Create messages with different priorities
        var priorities = new[]
        {
            MessagePriority.Normal,
            MessagePriority.High,
            MessagePriority.Low,
            MessagePriority.VeryHigh,
            MessagePriority.Lowest,
            MessagePriority.Highest
        };
        
        foreach (var priority in priorities)
        {
            // Create a message
            var message = new ExampleCommand();
            
            // Create a message context with the priority
            var context = new MessageContext { Priority = priority };
            
            // Create a message envelope
            var envelope = new MessageEnvelope<ExampleCommand>(
                message,
                context,
                sender,
                new AvionInternalReceiver(null, null))
            {
                Progress = new MessageProgress(new Created())
            };
            
            Console.WriteLine($"Sending message with priority {priority}");
            
            // Send the message
            await sender.Send(envelope);
            
            // Wait a bit between messages
            await Task.Delay(100);
        }
    }
}
