namespace AvionRelay.Core.Dispatchers;

public record MessageReceiver
{
    public string ReceiverId { get; set; }
    public string? Name { get; set; }

    public MessageReceiver()
    {
        
    }
    
    public MessageReceiver(string receiverId, string? name)
    {
        ReceiverId = receiverId;
        Name = name;
    }
}