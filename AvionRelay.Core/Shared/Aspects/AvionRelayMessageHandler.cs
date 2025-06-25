using Metalama.Framework.Aspects;

namespace AvionRelay.Core.Aspects;

public class AvionRelayMessageHandlerAttribute : OverrideMethodAspect
{
    /// <inheritdoc />
    public override Task<dynamic?> OverrideAsyncMethod()
    {
        try
        {
            meta.This._logger.LogInformation("Starting message handler {MessageHandler}", meta.Target.Method.ToDisplayString());
            return meta.Proceed();
        }
        finally
        {
            meta.This._logger.LogInformation("Completed message handler {MessageHandler}", meta.Target.Method.ToDisplayString());
        }
    }

    /// <inheritdoc />
    public override dynamic? OverrideMethod() => null;
}




