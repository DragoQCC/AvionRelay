using System.Diagnostics;
using AvionRelay.Core.Services;
using AvionRelay.External.Transports.SignalR;
using Scalar.AspNetCore;
using AvionRelay.Examples.SharedLibrary;
using AvionRelay.Examples.SharedLibrary.Commands;
using AvionRelay.External;
using GetLanguageInspection = AvionRelay.Examples.SharedLibrary.Inspections.GetLanguageInspection;

namespace AvionRelay.Examples.WebApp;

public class Program
{
    
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();
        
        builder.Services.AddOpenApi();
        
        //add as a SignalR client
        builder.Services.AddAvionRelayExternalMessaging().WithSignalRMessageBus(opt => 
        {
            opt.HubUrl = "https://localhost:7008/avionrelay";
        });
        
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapScalarApiReference();

        app.UseHttpsRedirection();
        app.UseAuthorization();

        //Create the list of message types this client can handle
        List<string> supportedMessages = [nameof(GetStatusCommand), nameof(AccessDeniedAlert),nameof(GetLanguageInspection)  ]; //
        //call the use external messaging, passing in the client config
        AvionRelayClientOptions clientOptions = new AvionRelayClientOptions()
        {
            Name = "Example Web App",
            ClientVersion = "1.0.0",
            SupportedMessageNames = supportedMessages
        };
        await app.UseAvionRelayExternalMessaging(clientOptions);
        
        await RegisterKnownHandler(app.Services);
        
       
        await app.RunAsync();
    }
    
    
    private static async Task RegisterKnownHandler(IServiceProvider serviceProvider)
    {
        //get the bus and the logger
        var bus = serviceProvider.GetRequiredService<AvionRelayExternalBus>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageHandler>>();
        
        //Create the handler class instances if non-static 
        var handler = new MessageHandler(bus, logger);

        //Register the handler
        await MessageHandlerRegister.RegisterHandler<GetStatusCommand>(handler.Receiver, handler.HandleGetStatusCommand);
        await MessageHandlerRegister.RegisterHandler<AccessDeniedAlert>(handler.Receiver, handler.HandleAccessDeniedAlert);
        await MessageHandlerRegister.RegisterHandler<GetLanguageInspection>(handler.Receiver, handler.HandleGetLanguageInspection);
    }

    [Conditional("DEBUG")]
    private static void PrintDiscoveredMessageTypes(WebApplication app)
    {
        var typeResolver = app.Services.GetRequiredService<ITypeResolver>();
        var messageType = typeResolver.GetType(nameof(GetStatusCommand));
        if (messageType is not null)
        {
            Console.WriteLine($"Successfully found message type  - {messageType.Name}: {messageType.AssemblyQualifiedName}");
        }

        var messageTypes = typeResolver.GetAllMessageTypes();
        foreach (var msgType in messageTypes)
        {
            Console.WriteLine($"Type resolver registered: {msgType.Name} : {msgType.AssemblyQualifiedName}");
        }
    }
}