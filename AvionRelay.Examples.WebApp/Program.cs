using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages.MessageTypes;
using AvionRelay.Core.Services;
using AvionRelay.External.Transports.SignalR;
using Scalar.AspNetCore;
using AvionRelay.Examples.SharedLibrary;
using AvionRelay.Examples.SharedLibrary.Commands;

namespace AvionRelay.Examples.WebApp;

public class Program
{
    private static IServiceProvider _serviceProvider;
    
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();
        
        builder.Services.AddOpenApi();
        
        //add as a SignalR client
        builder.Services.WithSignalRMessageBus(opt => 
        {
            opt.HubUrl = "https://localhost:7008/avionrelay";
            opt.ClientName = "Example Web App";
        });
        
        

        var app = builder.Build();
        
        _serviceProvider = app.Services;

       

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapScalarApiReference();

        app.UseHttpsRedirection();
        app.UseAuthorization();

        //get the SignalR message bus and connect it
        var signalRMessageBus = app.Services.GetRequiredService<AvionRelaySignalRMessageBus>();
        await signalRMessageBus.StartAsync();

        await RegisterKnownHandler();
        
        
       
        await app.RunAsync();
    }
    
    
    private static async Task RegisterKnownHandler()
    {
        //get the bus and the logger
        var bus = _serviceProvider.GetRequiredService<AvionRelayMessageBus>();
        var logger = _serviceProvider.GetRequiredService<ILogger<CommandHandler>>();
        var alertLogger = _serviceProvider.GetRequiredService<ILogger<AlertHandler>>();
        
        //Create the handler class instances if non-static 
        var handler = new CommandHandler(bus, logger);
        var alertHandler = new AlertHandler(bus, alertLogger);
        
        //Create MessageReceivers
        var receiver = new MessageReceiver(CommandHandler.HandlerID.ToString(), nameof(CommandHandler));
        var alertReceiver = new MessageReceiver(AlertHandler.HandlerID.ToString(), nameof(AlertHandler));
        
        //Register the handlers
        await MessageHandlerRegister.RegisterHandler<GetStatusCommand>(receiver,handler.HandleGetStatusCommand);
        await MessageHandlerRegister.RegisterHandler<AccessDeniedAlert>(alertReceiver,alertHandler.HandleAccessDeniedAlert);
        
        await bus.RegisterMessenger([nameof(GetStatusCommand), nameof(AccessDeniedAlert)]);
    }
}