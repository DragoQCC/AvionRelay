using System.Collections.ObjectModel;
using AvionRelay.Examples.SharedLibrary;
using AvionRelay.Examples.SharedLibrary.Commands;
using AvionRelay.External;
using GetLanguageInspection = AvionRelay.Examples.SharedLibrary.Inspections.GetLanguageInspection;
using LanguageInspectionResponse = AvionRelay.Examples.SharedLibrary.Inspections.LanguageInspectionResponse;

namespace AvionRelay.Examples.BlazorApp.Components.Pages;

public partial class Home
{
    private AvionRelayExternalBus _messageBus;

    private ResponsePayload<StatusResponse>? _statusResponse = null;
    string? targetHandler = null;
    
    private List<string>? inspectionHandlers = null;
    private ObservableCollection<ResponsePayload<LanguageInspectionResponse>>? inspectionResponses = null;

    public Home(AvionRelayExternalBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task SendGetStatusCommand()
    {
        //NON PROTOBUF VERSION
        SharedLibrary.Commands.GetStatusCommand statusCommand = new()
        {
            IncludeDetails = true
        };
        var commandResult = await _messageBus.ExecuteCommand<GetStatusCommand,StatusResponse>(statusCommand,targetHandler);
        _statusResponse = commandResult;
    }

    public async Task SendGetLanguageInspection()
    {
       
        inspectionResponses = null;
        GetLanguageInspection langInspection = new GetLanguageInspection();
        await foreach (var inspectionResponse in _messageBus.RequestInspection<GetLanguageInspection, LanguageInspectionResponse>(langInspection, [ "Example web app", "Python-Client-1" ]))
        {
            if (inspectionResponses is null)
            {
                inspectionResponses = [ ];
            }
            inspectionResponses.Add(inspectionResponse);
        }
        Console.WriteLine($"Got back {inspectionResponses?.Count} responses");
    }
    
    public async Task SendAccessDeniedAlert()
    {
        AccessDeniedAlert accessAlert = new(new User("Bob"),"Insufficient permissions");
        await _messageBus.SendAlert(accessAlert);
    }

    public async Task SendUserTerminationNotification()
    {
        UserTerminationNotification notif = new(new User("Bob"),"Repeated access attempts to secure systems");
        await _messageBus.PublishNotification(notif);
    }
}