namespace AvionRelay.Core.Messages;

/// <summary>
/// The Priority of a message <br/>
/// The Higher the value, the higher the priority. <br/>
/// Higher priority messages are processed before lower priority messages.
/// </summary>
public enum MessagePriority
{
    Lowest = 0,
    Low = 1,
    Normal = 2,
    High = 3,
    VeryHigh = 4,
    Highest = 5
}