namespace AvionRelay.Core.Messages;

/// <summary>
/// Represents an event that can be broadcast to many subscribers. No enforced response.
/// </summary>
public interface IEvent : IAvionRelayMessage
{
    
}