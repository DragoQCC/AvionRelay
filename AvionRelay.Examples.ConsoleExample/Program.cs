using AvionRelay.Core;
using AvionRelay.Core.Aspects;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;
using AvionRelay.Core.Services;
using AvionRelay.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AvionRelay.Examples.SharedLibrary;

namespace AvionRelay.Examples.ConsoleExample;

internal class Program
{
    private static IServiceProvider _serviceProvider;
    
    private static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        Setup(builder.Services);
        builder.Services.AddSingleton<InternalMessageSendingExample>();
        
        IHost host = builder.Build();
        await host.StartAsync();

        _serviceProvider = host.Services;
        
        //TODO: This is for testing
        await RegisterKnownHandler();
        
        var example = host.Services.GetRequiredService<InternalMessageSendingExample>();
        await example.RunExample();
        
        await host.WaitForShutdownAsync();
    }

    private static void Setup(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        
        // Add the message processing system
        services.AddAvionRelayCore(opt =>
            {
                opt.WithDefaultTimeout(TimeSpan.FromSeconds(5));
            }
        )
        .WithInMemoryStorage()
        .WithInternalMessageDispatcher();
    }

    private static async Task RegisterKnownHandler()
    {
        /*//get the bus and the logger
        var bus = _serviceProvider.GetRequiredService<AvionRelayMessageBus>();
        var logger = _serviceProvider.GetRequiredService<ILogger<ExampleMessageHandler>>();
        var secondLogger = _serviceProvider.GetRequiredService<ILogger<SecondExampleMessageHandler>>();
        
        //Create the handler class instances if non-static 
        var handler = new ExampleMessageHandler(bus, logger);
        var secondHandler = new SecondExampleMessageHandler(bus, secondLogger);
        
        //Create MessageReceivers
        var receiver = new MessageReceiver(ExampleMessageHandler.HandlerID.ToString(), nameof(ExampleMessageHandler));
        var secondReceiver = new MessageReceiver(SecondExampleMessageHandler.HandlerID.ToString(), nameof(SecondExampleMessageHandler));
        
        //Register the handlers
        await MessageHandlerRegister.RegisterHandler<CreateUserCommand>(receiver,handler.HandleCreateUserCommand);
        await MessageHandlerRegister.RegisterHandler<GetAllUsersInspection>(receiver,handler.HandleGetAllUsersInspection);
        await MessageHandlerRegister.RegisterHandler<AccessDeniedAlert>(receiver,handler.HandleAccessDeniedAlert);
        await MessageHandlerRegister.RegisterHandler<UserTerminationNotification>(receiver,handler.HandleUserTerminatedNotification);
        await MessageHandlerRegister.RegisterHandler<GetAllUsersInspection>(secondReceiver,secondHandler.HandleGetAllUsersInspection);
        await MessageHandlerRegister.RegisterHandler<UserTerminationNotification>(secondReceiver,secondHandler.HandleUserTerminatedNotification);*/
    }
}