using AvionRelay.Core.Aspects;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Examples.SharedLibrary.Commands;

namespace AvionRelay.Examples.WebApp;

public class CommandHandler
{
    private readonly AvionRelayMessageBus _bus;
    private readonly ILogger<CommandHandler> _logger;

    public static readonly Guid HandlerID = Guid.CreateVersion7();

    public CommandHandler(AvionRelayMessageBus bus, ILogger<CommandHandler> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    [AvionRelayMessageHandler]
    public async Task<StatusResponse> HandleGetStatusCommand(GetStatusCommand statusCommand)
    {
        _logger.LogInformation("Handling GetStatusCommand: {StatusCommand}", statusCommand);

        StatusResponse response = new StatusResponse();
        if (statusCommand.IncludeDetails)
        {
            response.Details = new()
            {
                {
                    "hostname", Environment.MachineName
                }
            };
        }
        response.Status = "testing";

        MessageReceiver receiver = new()
        {
            ReceiverId = HandlerID.ToString(),
            Name = nameof(CommandHandler)
        };
        await _bus.RespondToMessage<GetStatusCommand,StatusResponse>(statusCommand.Metadata.MessageId,response,receiver);
        return response;
    }
    
}