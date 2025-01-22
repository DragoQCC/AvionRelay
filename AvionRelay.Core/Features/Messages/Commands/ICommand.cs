namespace AvionRelay.Core.Messages.Commands;

/// <summary>
/// Represents a command, typically one-to-one, where the receiving side 
/// performs an action. May or may not return a result.
/// </summary>
public interface ICommand : IMessage
{
    
}