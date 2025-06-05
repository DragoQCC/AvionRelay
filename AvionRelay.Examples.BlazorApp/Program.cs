using AvionRelay.Examples.BlazorApp.Components;
using AvionRelay.External.Transports.SignalR;
using MudBlazor.Services;

namespace AvionRelay.Examples.BlazorApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMudServices();

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        //add as a SignalR client
        builder.Services.WithSignalRMessageBus(opt => 
        {
            opt.HubUrl = "https://localhost:7008/avionrelay";
            opt.ClientName = "Example Blazor App";
        });
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        
        //get the SignalR message bus and connect it
        var signalRMessageBus = app.Services.GetRequiredService<AvionRelaySignalRMessageBus>();
        await signalRMessageBus.StartAsync();
        await signalRMessageBus.RegisterMessenger();

        await app.RunAsync();
    }
}