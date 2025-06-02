using AvionRelay.Core.Dispatchers;

namespace AvionRelay.Core.Messages;

public class Package
{
    /// <summary>
    /// Type info for the message
    /// </summary>
    public string MessageType { get;  set; }
    
    /// <summary>
    /// The message itself
    /// </summary>
    public AvionRelayMessage Message { get; set; }
    
    /// <summary>
    /// Matches the ID of its message, used to correlate the two
    /// </summary>
    public Guid WrapperID { get;  set; }
    

    public static Package Create<TMessage>(TMessage message) where TMessage : AvionRelayMessage 
    {
        var package = new Package();
        package.Message = message;
        package.WrapperID = message.Metadata.MessageId;
        package.MessageType = typeof(TMessage).Name;
        return package;
    }
}

