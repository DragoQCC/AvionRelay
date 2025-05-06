using AvionRelay.Core;
using AvionRelay.Core.Handlers;
using AvionRelay.Core.Handlers.StateProcessors;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;
using AvionRelay.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleExample;

class Program
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        Setup(builder.Services);
        IHost host = builder.Build();
        await host.StartAsync();
        
        //await messageServiceExample.RunExample();
        
        await InternalMessageBrokerExample.RunExample();
        
        await host.WaitForShutdownAsync();
    }

    static void Setup(IServiceCollection services)
    {
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add the message processing system
        services.AddAvionRelayCore();
    }
}