namespace AvionRelay.Core;

public static partial class AvionRelayCoreExtensions
{
    /// <summary>
    /// Interface for tasks that run at startup.
    /// </summary>
    private interface IStartupTask
    {
        void Execute();
    }
}