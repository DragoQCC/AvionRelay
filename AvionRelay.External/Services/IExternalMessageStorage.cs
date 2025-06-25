using AvionRelay.Core.Messages;

namespace AvionRelay.External;

public interface IExternalMessageStorage
{
    public Task StoreTransportPackage(TransportPackage transportPackage);

    public Task StoreMessageContext(MessageContext messageContext);

    public Task StoreMessageForSchedule();

    public Task StoreMessageForFailure();

}