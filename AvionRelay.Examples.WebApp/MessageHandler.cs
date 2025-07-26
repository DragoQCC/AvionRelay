using AvionRelay.Core.Dispatchers;
using AvionRelay.Examples.SharedLibrary;
using AvionRelay.Examples.SharedLibrary.Commands;
using AvionRelay.Examples.SharedLibrary.Inspections;
using AvionRelay.External;

namespace AvionRelay.Examples.WebApp;

public class MessageHandler
{
    private readonly AvionRelayExternalBus _bus;
    private readonly ILogger<MessageHandler> _logger;

    public MessageReceiver Receiver { get; private set; }

    public MessageHandler(AvionRelayExternalBus bus, ILogger<MessageHandler> logger)
    {
        _bus = bus;
        _logger = logger;

        Receiver = new(_bus.AvionRelayClient.ClientID.ToString(), _bus.AvionRelayClient.ClientName);
    }
    

    public async Task HandleGetLanguageInspection(GetLanguageInspection languageInspection)
    {
        _logger.LogInformation("Handling GetLanguageInspection: {LanguageInspection}", languageInspection);

        //this should cause the server awaiting a response to timeout
        //await Task.Delay(60000);

        LanguageInspectionResponse response = new LanguageInspectionResponse()
        {
            Language = "C#"
        };
        
        await _bus.RespondToMessage<GetLanguageInspection,LanguageInspectionResponse>(languageInspection.Metadata.MessageId, response, Receiver);
    }
    

    public async Task<StatusResponse> HandleGetStatusCommand(GetStatusCommand statusCommand)
    {
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
        
        await _bus.RespondToMessage<GetStatusCommand,StatusResponse>(statusCommand.Metadata.MessageId, response, Receiver);
        return response;
    }
    

    public async Task HandleAccessDeniedAlert(AccessDeniedAlert alert)
    {
        _logger.LogInformation("Handling AccessDeniedAlert, Name: {Name}, Reason: {Reason} ", alert.User.Name, alert.Reason);
        await _bus.AcknowledgeMessage<AccessDeniedAlert>(alert.Metadata.MessageId, Receiver);
    }


    public async Task HandleUserTerminatedNotification(UserTerminationNotification notif)
    {
        _logger.LogInformation("Handling UserTerminatedNotification, Name: {Name}, Reason: {Reason}", notif.User.Name, notif.Reason);
    }
    
}