using Microsoft.Extensions.Logging;

namespace AvionRelay.Core;

/// <summary>
/// Uses the Microsoft logging extensions to generate logging methods
/// </summary>
public static partial class AvionRelayLogging
{
    [LoggerMessage(1, LogLevel.Information, "Message of Type {messageType} Created")]
    public static partial void MessageCreated(this ILogger logger, Type messageType);


}