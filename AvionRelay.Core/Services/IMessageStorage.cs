using AvionRelay.Core.Messages;

namespace AvionRelay.Core.Services;

public interface IMessageStorage
{
    public void StorePackage(Package package,bool inQueue);
    
    public Package? RetrieveNextPackage();
    public Package? RetrievePackage(Guid messageId);
    
}