using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Services;
using AvionRelay.Examples.SharedLibrary;
using Serilog;


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
        Log.Logger.Information("Starting Internal Message Broker Example");
        Log.Logger.Information("=======================================\n");
        
        // Send a message and wait for a response
        await SendCommand();
        // Wait for all messages to be processed
        await Task.Delay(1000);
        Console.WriteLine();
        Log.Logger.Information("=======================================\n");
        
        await PublishNotification();
        await Task.Delay(1000);
        Console.WriteLine();
        Log.Logger.Information("=======================================\n");
        
        await SendAlert();
        await Task.Delay(1000);
        Console.WriteLine();
        Log.Logger.Information("=======================================\n");
        
        await SendInspection();
        await Task.Delay(1000);
        Console.WriteLine();
        Log.Logger.Information("=======================================\n");
        
        Log.Logger.Information("All messages sent");
        Log.Logger.Information("\nInternal Message Broker Example complete!");
    }
    
    
    private async Task SendCommand()
    {
        Log.Logger.Information("\nSending a message and waiting for a response...");
        
        // Create a message
        CreateUserCommand userCommand = new CreateUserCommand(new User("Bob"));
        // Send the message and wait for a response
        var response = await _bus.ExecuteCommand<CreateUserCommand,UserCreated>(userCommand);
        
        bool wasAcknowledged = userCommand.Metadata.State is MessageState.AcknowledgementReceived or MessageState.ResponseReceived;
        Log.Logger.Information("Message acknowledged: {WasAcknowledged}", wasAcknowledged);
        Log.Logger.Information("Received response from: {AcknowledgerReceiverId}: {ResponseResponse}", response.Acknowledger.ReceiverId, response.Response);
    }
    
    private async Task PublishNotification()
    {
        Log.Logger.Information("\nSending a notification...");
        
        // Create a message
        UserTerminationNotification notification = new UserTerminationNotification(new User("Bob"), "Unauthorized access attempts, resulted in termination");
        
        // Send the message
        await _bus.PublishNotification(notification);
        
        //wait 1 second and then verify the message was acknowledged
        await Task.Delay(100);
        List<Acknowledgement> acks = notification.Metadata.Acknowledgements;
        Log.Logger.Information("Notification acknowledged by {AcksCount} / {GetReceiverCount}", acks.Count, MessageHandlerRegister.GetReceiverCount(notification.GetType()));
        Log.Logger.Information("Acks: {Join}", string.Join("\n", acks));
    }
    
    private async Task SendAlert()
    {
        Log.Logger.Information("\nSending an alert...");
        
        // Create a message
        AccessDeniedAlert alert = new AccessDeniedAlert(new User("Bob"), "Bob is not allowed to access this system");
        // Send the message
        await _bus.SendAlert(alert);
        //wait 1 second and then verify the message was acknowledged
        await Task.Delay(100);
        bool wasAcknowledged = alert.Metadata.State is MessageState.AcknowledgementReceived;
        Log.Logger.Information("Alert acknowledged: {WasAcknowledged}", wasAcknowledged);
        Log.Logger.Information("Ack Info: {FirstOrDefault}", alert.Metadata.Acknowledgements.FirstOrDefault());
    }

    private async Task SendInspection()
    {
        Log.Logger.Information("\nSending an inspection...");

        // Create a message
        GetAllUsersInspection inspection = new GetAllUsersInspection();
        // Send the message
        var responses = await _bus.RequestInspection<GetAllUsersInspection, List<User>>(inspection);
        
        List<Acknowledgement> acks = inspection.Metadata.Acknowledgements;
        Log.Logger.Information("Inspection acknowledged by {AcksCount} / {GetReceiverCount} receivers", acks.Count, MessageHandlerRegister.GetReceiverCount(inspection.GetType()));
        foreach (var response in responses)
        {
            //Write the acknowledgement info
            Log.Logger.Information("Response from {AcknowledgerName}:", response.Acknowledger.Name);
            //Write the response
            foreach (var user in response.Response)
            {
                Log.Logger.Information("\t{User}", user);
            }
        }
    }
}
