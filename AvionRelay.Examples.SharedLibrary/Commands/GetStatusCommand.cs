using AvionRelay.Core.Messages.MessageTypes;

namespace AvionRelay.Examples.SharedLibrary.Commands;

public record GetStatusCommand : Command<StatusResponse>
{
    public bool IncludeDetails { get; set; }
}

public record StatusResponse
{
    public string Status { get; set; } = "Running";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object>? Details { get; set; }
}