using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;

namespace AvionRelay.External;

/// <summary>
/// Wrapper that preserves type information for responses sent through SignalR
/// </summary>
public class JsonResponse : IMessageResponse
{
    /// <inheritdoc />
    public Guid MessageId { get; set; }
    
    /// <inheritdoc />
    public MessageReceiver Acknowledger { get; set; }
    
    public string ResponseJson { get; set; } = string.Empty;
}