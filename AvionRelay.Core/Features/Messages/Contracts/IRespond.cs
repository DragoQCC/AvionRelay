namespace AvionRelay.Core.Messages;

public interface IRespond<TResponse>
{
    public Task Respond(TResponse response);
}