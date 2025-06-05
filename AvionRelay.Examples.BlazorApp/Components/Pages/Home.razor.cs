using AvionRelay.Core.Dispatchers;
using AvionRelay.Examples.SharedLibrary;
using AvionRelay.Examples.SharedLibrary.Commands;

namespace AvionRelay.Examples.BlazorApp.Components.Pages;

public partial class Home
{
    private AvionRelayMessageBus _messageBus;

    private StatusResponse? _statusResponse = null;

    public Home(AvionRelayMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task SendGetStatusCommand()
    {
        GetStatusCommand statusCommand = new()
        {
            IncludeDetails = true
        };
        var commandResult = await _messageBus.ExecuteCommand<GetStatusCommand,StatusResponse>(statusCommand);
        _statusResponse = commandResult.Response;
    }

    public async Task SendAccessDeniedAlert()
    {
        AccessDeniedAlert accessAlert = new(new User("Bob"),"Insufficient permissions");
        await _messageBus.SendAlert(accessAlert);
    }
}