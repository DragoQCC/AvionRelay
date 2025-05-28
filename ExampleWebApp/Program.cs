using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Services;
using AvionRelay.External.Transports.SignalR;
using Scalar.AspNetCore;

namespace ExampleWebApp;

public class Program
{
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
            opt.ClientName = "ExampleWebApp";
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

        //get the SignalR message bus and connect it
        var signalRMessageBus = app.Services.GetRequiredService<AvionRelaySignalRMessageBus>();
        await signalRMessageBus.StartAsync();
       
        await app.RunAsync();
    }
}