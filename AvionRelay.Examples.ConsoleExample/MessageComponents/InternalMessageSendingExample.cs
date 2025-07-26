using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using AvionRelay.Examples.SharedLibrary;


namespace AvionRelay.Examples.ConsoleExample;

/// <summary>
/// Example demonstrating the internal message broker.
/// </summary>
public class InternalMessageSendingExample
{
    private readonly AvionRelayMessageBus _bus;
    private readonly IMessageStorage _storage;

    public InternalMessageSendingExample(IMessageStorage storage, AvionRelayMessageBus bus)
    {
        _storage = storage;
        _bus = bus;
    }
    
    /// <summary>
    /// Runs the example.
    /// </summary>
    public async Task RunExample()
    {
        Console.WriteLine("Starting Internal Message Broker Example");
        Console.WriteLine("=======================================\n");
        
        // Send a message and wait for a response
        await SendCommand();
        // Wait for all messages to be processed
        await Task.Delay(1000);
        Console.WriteLine();
        Console.WriteLine("=======================================\n");
        
        await PublishNotification();
        await Task.Delay(1000);
        Console.WriteLine();
        Console.WriteLine("=======================================\n");
        
        await SendAlert();
        await Task.Delay(1000);
        Console.WriteLine();
        Console.WriteLine("=======================================\n");
        
        await SendInspection();
        await Task.Delay(1000);
        Console.WriteLine();
        Console.WriteLine("=======================================\n");
        
        Console.WriteLine("All messages sent");
        Console.WriteLine("\nInternal Message Broker Example complete!");
    }
    
    
    private async Task SendCommand()
    {
        Console.WriteLine("\nSending a message and waiting for a response...");
        
        // Create a message
        CreateUserCommand userCommand = new CreateUserCommand(new User("Bob"));
        // Send the message and wait for a response
        var response = await _bus.ExecuteCommand<CreateUserCommand,UserCreated>(userCommand);
        
        bool wasAcknowledged = userCommand.Metadata.State is MessageState.AcknowledgementReceived or MessageState.ResponseReceived;
        Console.WriteLine($"Message acknowledged: {wasAcknowledged}");
        Console.WriteLine($"Received response from: {response.Acknowledger.ReceiverId}: {response.Response}");
    }
    
    private async Task PublishNotification()
    {
        Console.WriteLine("\nSending a notification...");
        
        // Create a message
        UserTerminationNotification notification = new UserTerminationNotification(new User("Bob"), "Unauthorized access attempts, resulted in termination");
        
        // Send the message
        await _bus.PublishNotification(notification);
        
        //wait 1 second and then verify the message was acknowledged
        await Task.Delay(100);
        List<Acknowledgement> acks = notification.Metadata.Acknowledgements;
        Console.WriteLine($"Notification acknowledged by {acks.Count} / {MessageHandlerRegister.GetReceiverCount(notification.GetType())}");
        Console.WriteLine($"Acks: {string.Join("\n", acks)}");
    }
    
    private async Task SendAlert()
    {
        Console.WriteLine("\nSending an alert...");
        
        // Create a message
        AccessDeniedAlert alert = new AccessDeniedAlert(new User("Bob"), "Bob is not allowed to access this system");
        // Send the message
        await _bus.SendAlert(alert);
        //wait 1 second and then verify the message was acknowledged
        await Task.Delay(100);
        bool wasAcknowledged = alert.Metadata.State is MessageState.AcknowledgementReceived;
        Console.WriteLine($"Alert acknowledged: {wasAcknowledged}");
        Console.WriteLine($"Ack Info: {alert.Metadata.Acknowledgements.FirstOrDefault()}");
    }

    private async Task SendInspection()
    {
        Console.WriteLine("\nSending an inspection...");

        // Create a message
        GetAllUsersInspection inspection = new GetAllUsersInspection();
        // Send the message
        var responses = await _bus.RequestInspection<GetAllUsersInspection, List<User>>(inspection);
        
        List<Acknowledgement> acks = inspection.Metadata.Acknowledgements;
        Console.WriteLine($"Inspection acknowledged by {acks.Count} / {MessageHandlerRegister.GetReceiverCount(inspection.GetType())} receivers");
        foreach (var response in responses)
        {
            //Write the acknowledgement info
            Console.WriteLine($"Response from {response.Acknowledger.Name}:");
            //Write the response
            foreach (var user in response.Response)
            {
                Console.WriteLine($"\t{user}");
            }
        }
    }
}
