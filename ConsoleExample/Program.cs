using AvionRelay.Core.Messages.MessageTypes;

namespace ConsoleExample;

class Program
{
    static async Task Main(string[] args)
    {
        CreateUserCommand createUserCommand = new();
        createUserCommand.Acknowledge();
        await createUserCommand.Respond(new CreatedUserResponse("123"));
        Console.WriteLine($"Was Acknowledged: " + createUserCommand.IsAcknowledged);
    }
}

public record CreatedUserResponse(string UserId);

public class CreateUserCommand : Command<CreatedUserResponse>
{
    
}