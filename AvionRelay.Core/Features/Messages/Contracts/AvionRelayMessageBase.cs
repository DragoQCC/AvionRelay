namespace AvionRelay.Core.Messages;

public abstract class AvionRelayMessageBase : IAvionRelayMessage
{
    public Guid MessageId { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}