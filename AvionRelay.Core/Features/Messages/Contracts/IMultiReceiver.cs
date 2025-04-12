using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages;

/// <summary>
/// Indicates that a message is intended for multiple receivers.
/// </summary>
public interface IMultiReceiver
{
    List<MessageReceiver> Receivers { get; }
}