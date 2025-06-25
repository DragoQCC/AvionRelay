namespace AvionRelay.Core.Messages;

/// <summary>
/// The Priority of a message <br/>
/// The Higher the value, the higher the priority. <br/>
/// Higher priority messages are processed before lower priority messages.
/// </summary>
public enum MessagePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    VeryHigh = 3,
    Critical = 4
}