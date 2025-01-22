namespace AvionRelay.Core.Messages.Events;

/// <summary>
/// Represents an event that can be broadcast to many subscribers. No enforced response.
/// </summary>
public interface IEvent : IMessage
{
    
}