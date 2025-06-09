using AvionRelay.Core.Aspects;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Examples.SharedLibrary;

namespace AvionRelay.Examples.WebApp;

public class AlertHandler
{
    private readonly AvionRelayMessageBus _bus;
    private readonly ILogger<AlertHandler> _logger;

    public static readonly Guid HandlerID = Guid.CreateVersion7();
    
    public AlertHandler(AvionRelayMessageBus bus, ILogger<AlertHandler> logger)
    {
        _bus = bus;
        _logger = logger;
    }
    
    [AvionRelayMessageHandler]
    public async Task HandleAccessDeniedAlert(AccessDeniedAlert alert)
    {
        _logger.LogInformation("Handling AccessDeniedAlert: {Alert}", alert);
        var responder = new MessageReceiver()
        {
            ReceiverId = HandlerID.ToString(),
            Name = nameof(AlertHandler)
        };
        await _bus.AcknowledgeMessage<AccessDeniedAlert>(alert.Metadata.MessageId, responder);
    }
}