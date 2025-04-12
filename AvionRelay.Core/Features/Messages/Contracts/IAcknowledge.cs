namespace AvionRelay.Core.Messages;

public interface IAcknowledge
{
    public bool IsAcknowledged { get; set; }

    public void Acknowledge();
}