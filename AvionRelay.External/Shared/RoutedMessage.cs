using AvionRelay.Core.Messages;

namespace AvionRelay.External;

public record RoutedMessage
{
    public required Package Package { get; init; }
    public required string SenderId { get; init; }
    public required Guid MessageId { get; init; }
    public string? TargetClientId { get; init; }
    public string? TargetGroup { get; init; }
}